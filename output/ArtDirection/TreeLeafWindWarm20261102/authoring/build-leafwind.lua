-- Production composition helper for Aseprite 1.3.18.6; see usage.md.
-- All art, masks, timing and motion are supplied by the graphics author.
local input = assert(app.params.input, "Missing --script-param input=...")
local outdir = assert(app.params.outdir, "Missing --script-param outdir=...")
local configPath = assert(app.params.config, "Missing --script-param config=...")
local file = assert(io.open(configPath, "r"))
local config = json.decode(file:read("*a"))
file:close()

local function integer(value, label)
  assert(type(value) == "number" and value == math.floor(value), label .. " must be integer")
  return value
end

local name = config.name or "tree-leafwind"
assert(type(name) == "string" and name:match("^[%w_-]+$"), "Invalid output name")
assert(app.fs.isDirectory(outdir), "Create outdir before running")
assert(config.lobes and #config.lobes > 0, "At least one lobe required")
assert(config.frames and #config.frames > 0, "At least one frame required")
local inset = integer(config.base_inset or 1, "base_inset")
assert(inset >= 0, "base_inset must be nonnegative")

local source = Image{ fromFile=input }
assert(source and source.colorMode == ColorMode.RGB, "Source must be RGBA")
local width, height = source.width, source.height
local pc = app.pixelColor
local function index(x, y) return y * width + x + 1 end
local function inBounds(x, y) return x >= 0 and y >= 0 and x < width and y < height end
local function newImage()
  local image = Image(source.spec)
  image:clear()
  return image
end
local function colorKey(r, g, b) return r * 65536 + g * 256 + b end
local woodColors = {}
for _, color in ipairs(config.wood_colors or {}) do
  assert(#color == 3, "wood_colors entries require RGB")
  for _, channel in ipairs(color) do
    integer(channel, "RGB channel")
    assert(channel >= 0 and channel <= 255, "RGB channel out of range")
  end
  woodColors[colorKey(color[1], color[2], color[3])] = true
end

local woodMask
if config.wood_mask then
  local path = config.wood_mask
  if not app.fs.isFile(path) then
    path = app.fs.joinPath(app.fs.filePath(configPath), path)
  end
  woodMask = Image{ fromFile=path }
  assert(woodMask and woodMask.colorMode == ColorMode.RGB, "wood_mask must be RGBA")
  assert(woodMask.width == width and woodMask.height == height, "wood_mask must match source canvas")
end
assert(next(woodColors) or woodMask, "Explicit wood_colors or wood_mask required")

-- Sample polygon membership at pixel centers. Overlapping lobes are intentional.
local function insidePolygon(x, y, polygon)
  local inside = false
  local previous = polygon[#polygon]
  for _, current in ipairs(polygon) do
    if (current[2] > y) ~= (previous[2] > y) then
      local crossing = (previous[1] - current[1]) * (y - current[2]) /
                       (previous[2] - current[2]) + current[1]
      if x < crossing then inside = not inside end
    end
    previous = current
  end
  return inside
end

local lobeNames, lobes = {}, {}
for _, definition in ipairs(config.lobes) do
  -- JSON userdata can coerce assigned packed pixel integers on readback.
  -- Keep runtime pixels in native Lua tables, never in the decoded config.
  local lobe = {name=definition.name, polygon=definition.polygon, pixels={}}
  assert(type(lobe.name) == "string" and #lobe.name > 0 and not lobeNames[lobe.name], "Lobe names must be unique")
  assert(lobe.polygon and #lobe.polygon >= 3, "Lobe requires a polygon")
  for _, point in ipairs(lobe.polygon) do
    assert(#point == 2, "Polygon vertices require x,y")
    integer(point[1], "Polygon x")
    integer(point[2], "Polygon y")
  end
  lobeNames[lobe.name] = true
  lobes[#lobes + 1] = lobe
end
for _, frame in ipairs(config.frames) do
  assert(type(frame.duration) == "number" and frame.duration >= 0.001 and frame.duration <= 65.535,
         "Frame duration must be 0.001..65.535 seconds")
  for lobeName, offset in pairs(frame.offsets or {}) do
    assert(lobeNames[lobeName], "Unknown offset lobe: " .. lobeName)
    assert(#offset == 2, "Offset requires dx,dy")
    integer(offset[1], "Offset dx")
    integer(offset[2], "Offset dy")
  end
end

local protected, opaque, covered = {}, {}, {}
local wood = newImage()
local protectedCount = 0
for y = 0, height - 1 do
  for x = 0, width - 1 do
    local pixel = source:getPixel(x, y)
    local i = index(x, y)
    opaque[i] = pc.rgbaA(pixel) > 0
    protected[i] = woodColors[colorKey(pc.rgbaR(pixel), pc.rgbaG(pixel), pc.rgbaB(pixel))] or
                   (woodMask and pc.rgbaA(woodMask:getPixel(x, y)) > 0) or false
    if protected[i] and opaque[i] then
      wood:drawPixel(x, y, pixel)
      protectedCount = protectedCount + 1
    elseif opaque[i] then
      for _, lobe in ipairs(lobes) do
        if insidePolygon(x + 0.5, y + 0.5, lobe.polygon) then
          lobe.pixels[#lobe.pixels + 1] = {x, y, pixel}
          covered[i] = true
        end
      end
    end
  end
end
assert(protectedCount > 0, "Wood selection matches no visible source pixels")
for _, lobe in ipairs(lobes) do
  assert(#lobe.pixels > 0, "Empty foliage lobe: " .. lobe.name)
end

-- Erode only mapped foliage silhouette, retaining interior coverage and unmapped art.
local interior = opaque
for _ = 1, inset do
  local nextInterior = {}
  for y = 0, height - 1 do
    for x = 0, width - 1 do
      local function retained(px, py)
        return inBounds(px, py) and interior[index(px, py)]
      end
      nextInterior[index(x, y)] = retained(x, y) and retained(x - 1, y) and
        retained(x + 1, y) and retained(x, y - 1) and retained(x, y + 1)
    end
  end
  interior = nextInterior
end
local base = newImage()
for y = 0, height - 1 do
  for x = 0, width - 1 do
    local i = index(x, y)
    if opaque[i] and not protected[i] and (not covered[i] or interior[i]) then
      base:drawPixel(x, y, source:getPixel(x, y))
    end
  end
end

-- Prepare every cel before saving, so bad offsets cannot leave partial exports.
local prepared = {}
for frameIndex, frame in ipairs(config.frames) do
  prepared[frameIndex] = {}
  for lobeIndex, lobe in ipairs(lobes) do
    local offset = (frame.offsets or {})[lobe.name] or {0, 0}
    local moved = newImage()
    for _, pixel in ipairs(lobe.pixels) do
      local x, y = pixel[1] + offset[1], pixel[2] + offset[2]
      assert(inBounds(x, y), "Motion clips canvas: frame " .. frameIndex .. ", " .. lobe.name)
      if not protected[index(x, y)] then moved:drawPixel(x, y, pixel[3]) end
    end
    prepared[frameIndex][lobeIndex] = moved
  end
end

local sprite = Sprite(source.spec)
local baseLayer = sprite.layers[1]
baseLayer.name = "Fixed canopy interior"
local layers = {}
for i, lobe in ipairs(lobes) do
  layers[i] = sprite:newLayer()
  layers[i].name = lobe.name
end
local woodLayer = sprite:newLayer()
woodLayer.name = "Fixed trunk branches roots"
for frameIndex, frame in ipairs(config.frames) do
  if frameIndex > 1 then sprite:newEmptyFrame(frameIndex) end
  sprite.frames[frameIndex].duration = frame.duration
  sprite:newCel(baseLayer, frameIndex, base, Point(0, 0))
  for i, layer in ipairs(layers) do
    sprite:newCel(layer, frameIndex, prepared[frameIndex][i], Point(0, 0))
  end
  sprite:newCel(woodLayer, frameIndex, wood, Point(0, 0))
end
sprite:saveAs(app.fs.joinPath(outdir, name .. ".aseprite"))
for frameIndex = 1, #config.frames do
  local flattened = newImage()
  flattened:drawSprite(sprite, frameIndex, Point(0, 0))
  flattened:saveAs(app.fs.joinPath(outdir, string.format("%s-frame-%02d.png", name, frameIndex)))
end
print(string.format("Authored %s: %dx%d, %d frames, %d foliage lobes, %d fixed wood pixels",
  name, width, height, #config.frames, #lobes, protectedCount))
