$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'output/시안/Terrain64-v03'
$src=Join-Path (Get-Location) 'Assets/Art/Generated/TerrainTest/terrain-tileset-64.png'
$old=[System.Drawing.Bitmap]::FromFile($src)
$atlas=$old.Clone([System.Drawing.Rectangle]::new(0,0,1024,768),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$map=Get-Content -Raw -Encoding UTF8 -LiteralPath 'output/시안/Terrain64/terrain-tileset-64.json' | ConvertFrom-Json
function Tile([int]$i) {return $old.Clone([System.Drawing.Rectangle]::new(($i%16)*64,([int][Math]::Floor($i/16))*64,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
$tiles=@{};foreach($i in @(0,32,120,126,132,138,144,145,147,150,151,153)) {$tiles[$i]=Tile $i}
$gen=[System.Drawing.Bitmap]::FromFile('C:/Users/Merkatte/.codex/generated_images/01a17248-a290-7c62-8413-fe5db02e5aac/exec-d48b5d10-ceeb-4c18-ac0b-081b448a75a4.png')
for($q=0;$q -lt 2;$q++) {$b=[System.Drawing.Bitmap]::new(64,64);$g=[System.Drawing.Graphics]::FromImage($b);$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half;$w=[int][Math]::Floor($gen.Width/2);$g.DrawImage($gen,[System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Rectangle]::new($q*$w,0,$w,$gen.Height),[System.Drawing.GraphicsUnit]::Pixel);$g.Dispose();$tiles[200+$q]=$b}
$gen.Dispose()
# Corrections apply to corner endpoints only; the central source artwork is not rebuilt.
# Each edge tuple is [edge, original straight neighbor, neighbor-facing edge, source-coordinate shift].
$defs=@(
 @{i=144;source=144;name='island_SE';edges=@(@('N',132,'S',-4),@('W',120,'E',5))},
 @{i=145;source=145;name='island_SW';edges=@(@('N',138,'S',-4),@('E',120,'W',-12))},
 @{i=146;source=200;name='island_NE';edges=@(@('S',132,'N',15),@('W',126,'E',-9))},
 @{i=147;source=201;name='island_NW';edges=@(@('S',138,'N',-15),@('E',126,'W',-9))},
 @{i=150;source=151;name='pool_NW';edges=@(@('E',120,'W',-10),@('S',132,'N',12))},
 @{i=151;source=153;name='pool_NE';edges=@(@('W',120,'E',-16),@('S',138,'N',11))},
 @{i=152;source=150;name='pool_SW';edges=@(@('N',132,'S',3),@('E',126,'W',19))},
 @{i=153;source=147;name='pool_SE';edges=@(@('N',138,'S',11),@('W',126,'E',20))}
)
$results=@{}
foreach($def in $defs) {
 $im=$tiles[$def.source];$b=[System.Drawing.Bitmap]::new(64,64)
 $origin=switch($def.i){144 {@(0,0)}145 {@(63,0)}146 {@(0,63)}147 {@(63,63)}150 {@(63,63)}151 {@(0,63)}152 {@(63,0)}153 {@(0,0)}}
 $ox=$origin[0];$oy=$origin[1];$signX=if($ox -eq 0){1}else{-1};$signY=if($oy -eq 0){1}else{-1}
 $landCenter=$def.i -lt 150
 $radii=[double[]]::new(181)
 for($angle=0;$angle -le 180;$angle++){
  $theta=$angle*[Math]::PI/360;$ux=[Math]::Cos($theta);$uy=[Math]::Sin($theta);$maxR=63/[Math]::Max($ux,$uy);$hit=$maxR-1;$run=0
  for($r=1;$r -le $maxR;$r++){
   $px=[int][Math]::Round($ox+$signX*$ux*$r);$py=[int][Math]::Round($oy+$signY*$uy*$r);$c=$im.GetPixel($px,$py)
   $grass=$c.G -gt $c.R*1.025 -and $c.G -gt $c.B*1.25
   $opposite=if($landCenter){$c.B -gt $c.R*1.5 -and $c.B -gt $c.G*1.05}else{$grass}
   if($opposite){$run++}else{$run=0}
   if($run -ge 2){$hit=$r-1;if($landCenter -and $angle -lt 90){$hit-=4};break}
  }
  $radii[$angle]=$hit
 }
 $smooth=$radii.Clone();for($a=0;$a -le 180;$a++){$list=@();for($q=[Math]::Max(0,$a-3);$q -le [Math]::Min(180,$a+3);$q++){$list+=$radii[$q]};$sorted=@($list|Sort-Object);$smooth[$a]=$sorted[[int][Math]::Floor($sorted.Count/2)]};$radii=$smooth
 for($y=0;$y -lt 64;$y++) {for($x=0;$x -lt 64;$x++) {
  $dx=[Math]::Abs($x-$ox);$dy=[Math]::Abs($y-$oy);$radius=[Math]::Sqrt($dx*$dx+$dy*$dy)
  if($radius -lt .01){$sx=$ox;$sy=$oy}else{
   $ux=$dx/$radius;$uy=$dy/$radius;$angle=[int][Math]::Round([Math]::Atan2($dy,$dx)*360/[Math]::PI);$sr=$radii[$angle];$maxR=63/[Math]::Max($ux,$uy)
   $mapped=if($radius -lt 20){$radius*[Math]::Max(1,$sr-12)/20}elseif($radius -le 44){$sr+$radius-32}else{$sr+12+($radius-44)*($maxR-$sr-12)/($maxR-44)}
   $mapped=[Math]::Max(0,[Math]::Min($maxR,$mapped))
   $sx=$ox+$signX*$ux*$mapped;$sy=$oy+$signY*$uy*$mapped
  }
  if($def.i -in @(144,145,151,153)) {
   $anchors=switch($def.i){144 {@(28,37,32,32)}145 {@(29,20,33,32)}151 {@(44,16,33,32)}153 {@(44,52,33,32)}}
   $ax=[double]$anchors[0];$ay=[double]$anchors[1];$tx=[double]$anchors[2];$ty=[double]$anchors[3]
   $sx=if($x -le $tx){$x*$ax/$tx}else{$ax+($x-$tx)*(63-$ax)/(63-$tx)}
   $sy=if($y -le $ty){$y*$ay/$ty}else{$ay+($y-$ty)*(63-$ay)/(63-$ty)}
  }
  $px=[int][Math]::Round($sx);$py=[int][Math]::Round($sy)
  if($px -lt 0 -or $px -gt 63 -or $py -lt 0 -or $py -gt 63) {
   $cx=[Math]::Max(0,[Math]::Min(63,$px));$cy=[Math]::Max(0,[Math]::Min(63,$py));$c=$im.GetPixel($cx,$cy)
   $isGrass=$c.G -gt $c.R*1.025 -and $c.G -gt $c.B*1.25
   $c=if($isGrass){$tiles[0].GetPixel($x,$y)}else{$tiles[32].GetPixel($x,$y)}
  } else {$c=$im.GetPixel($px,$py)}
  if($def.i -in @(146,147,150,152) -and ($radius -gt 37 -or $radius -lt 20)) {
   $wantedGrass=if($landCenter){$radius -lt 32}else{$radius -gt 32}
   $c=if($wantedGrass){$tiles[120].GetPixel($x,(($y+11)%24))}else{$tiles[32].GetPixel($x,$y)}
  }
  $b.SetPixel($x,$y,$c)
 }}
 # Keep the complete eight-pixel original neighbor endpoint band: grass lip, rock thickness and ripple together.
 foreach($e in $def.edges) {
  $neighbor=$tiles[[int]$e[1]]
  for($d=0;$d -lt 8;$d++) {for($p=0;$p -lt 64;$p++) {
   $tx=switch($e[0]){'W' {$d} 'E' {63-$d} default {$p}}
   $ty=switch($e[0]){'N' {$d} 'S' {63-$d} default {$p}}
   $nx=switch($e[2]){'W' {$d} 'E' {63-$d} default {$p}}
   $ny=switch($e[2]){'N' {$d} 'S' {63-$d} default {$p}}
   $b.SetPixel($tx,$ty,$neighbor.GetPixel($nx,$ny))
  }}
 }
 # Final polish: only unprotected grass well behind the two accepted right-bank corner corridors.
 if($def.i -in @(151,153)){
  for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
   $protected=if($def.i -eq 151){$x -lt 8 -or $y -ge 56}else{$x -lt 8 -or $y -lt 8}
   $dy=if($def.i -eq 151){63-$y}else{$y}
   $distance=[Math]::Sqrt($x*$x+$dy*$dy)
   if(-not $protected -and $distance -gt 37){$b.SetPixel($x,$y,$tiles[138].GetPixel((40+($x%24)),$y))}
  }}
 }
 $results[$def.i]=$b
 $g=[System.Drawing.Graphics]::FromImage($atlas);$g.DrawImageUnscaled($b,($def.i%16)*64,([int][Math]::Floor($def.i/16))*64);$g.Dispose()
 $t=$map.tiles[$def.i];$t.name='water_corner_'+$def.name;$t.notes='Physical placement within a pool or grass island; local original-art endpoint repair, not the former ambiguous quadrant naming.'
 $t.source=[pscustomobject]@{originalTile=$def.source;placement=$def.name;endpointBand=8;neighborEdges=$def.edges}
}
$atlas.Save((Join-Path $dir 'terrain-tileset-64-v03.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$map.image='terrain-tileset-64-v03.png'
$map.limitations=@('Only eight water corner cells changed; all184 other tiles remain unchanged.','Original textures and varied source lighting remain; no claim of globally seamless interior textures.','No Unity import or slicing performed.')
$map | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $dir 'terrain-tileset-64-v03.json') -Encoding UTF8
$proof=[System.Drawing.Bitmap]::new(1280,740);$pg=[System.Drawing.Graphics]::FromImage($proof);$pg.Clear([System.Drawing.Color]::FromArgb(20,26,37));$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half;$font=[System.Drawing.Font]::new('Consolas',15)
foreach($type in @('pool','island')) {
 $offset=if($type -eq 'pool'){0}else{640}
 $pg.DrawString($type.ToUpper()+' 64px tiles / 2x view',$font,[System.Drawing.Brushes]::White,$offset,0)
 for($r=0;$r -lt 5;$r++){for($c=0;$c -lt 5;$c++){
  if($type -eq 'pool') {
   $i=if($r -eq 0 -and $c -eq 0){150}elseif($r -eq 0 -and $c -eq 4){151}elseif($r -eq 4 -and $c -eq 0){152}elseif($r -eq 4 -and $c -eq 4){153}elseif($r -eq 0){120}elseif($r -eq 4){126}elseif($c -eq 0){132}elseif($c -eq 4){138}else{32}
  } else {
   $i=if($r -eq 0 -and $c -eq 0){147}elseif($r -eq 0 -and $c -eq 4){146}elseif($r -eq 4 -and $c -eq 0){145}elseif($r -eq 4 -and $c -eq 4){144}elseif($r -eq 0){126}elseif($r -eq 4){120}elseif($c -eq 0){138}elseif($c -eq 4){132}else{0}
  }
  $pg.DrawImage($atlas,[System.Drawing.Rectangle]::new($offset+$c*128,40+$r*128,128,128),[System.Drawing.Rectangle]::new(($i%16)*64,([int][Math]::Floor($i/16))*64,64,64),[System.Drawing.GraphicsUnit]::Pixel)
 }}
}
$pg.DrawString('POOL NW:r10c7 NE:r10c8 SW:r10c9 SE:r10c10',$font,[System.Drawing.Brushes]::White,0,690)
$pg.DrawString('ISLAND NW:r10c4 NE:r10c3 SW:r10c2 SE:r10c1',$font,[System.Drawing.Brushes]::White,640,690)
$pg.Dispose();$proof.Save((Join-Path $dir 'water-corners-proof.png'));$proof.Dispose();$font.Dispose()
$unchanged=0;for($i=0;$i -lt 192;$i++){if($i -in $defs.i){continue};$same=$true;for($y=0;$y -lt 64 -and $same;$y++){for($x=0;$x -lt 64;$x++){if($old.GetPixel(($i%16)*64+$x,([int][Math]::Floor($i/16))*64+$y).ToArgb() -ne $atlas.GetPixel(($i%16)*64+$x,([int][Math]::Floor($i/16))*64+$y).ToArgb()){$same=$false;break}}};if($same){$unchanged++}}
Write-Output "Staged1024x768; unchanged non-corner tiles:$unchanged/184"
$atlas.Dispose();$old.Dispose();foreach($b in $tiles.Values){$b.Dispose()};foreach($b in $results.Values){$b.Dispose()}
