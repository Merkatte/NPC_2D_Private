param([switch]$Pack)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$sourceRoot = Join-Path $root 'output/시안'
$tiles = [System.Collections.Generic.List[object]]::new()
$sources = @{}
function Open-Source([string]$name) {
    if (-not $sources.ContainsKey($name)) { $sources[$name] = [System.Drawing.Bitmap]::FromFile((Join-Path $sourceRoot $name)) }
    return $sources[$name]
}
function New-Bitmap([int]$width, [int]$height) {
    return [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}
function Get-Crop([string]$file, [int]$x, [int]$y, [int]$width, [int]$height) {
    $source = Open-Source $file
    $image = New-Bitmap 64 64
    $g = [System.Drawing.Graphics]::FromImage($image)
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($source, [System.Drawing.Rectangle]::new(0,0,64,64), $x,$y,$width,$height,[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    return $image
}
function Add-Tile([string]$name,[string]$family,$image,$sourceInfo,[string]$notes='') {
    $tiles.Add([pscustomobject]@{name=$name; family=$family; image=$image; source=$sourceInfo; notes=$notes})
}
function Mix-Color($a,$b,[double]$weight) {
    return [System.Drawing.Color]::FromArgb(255,[int][Math]::Round($a.R*(1-$weight)+$b.R*$weight),[int][Math]::Round($a.G*(1-$weight)+$b.G*$weight),[int][Math]::Round($a.B*(1-$weight)+$b.B*$weight))
}
function Is-Grass($color) { return ($color.R -gt 45 -and $color.G -gt $color.R*1.025 -and $color.G -gt $color.B*1.25) }
function Get-Anchor($image,[string]$direction,[int]$axis) {
    $positions = @()
    for($p=0;$p -lt 64;$p++) {
        if($direction -eq 'above' -or $direction -eq 'below') { $color=$image.GetPixel($axis,$p) } else { $color=$image.GetPixel($p,$axis) }
        if(Is-Grass $color) { $positions += $p }
    }
    if($positions.Count -eq 0) { return 32 }
    if($direction -eq 'above' -or $direction -eq 'left') { return [Math]::Min(63,($positions[-1]+1)) }
    return $positions[0]
}
function Align-Boundary($image,[string]$direction,[string]$otherFamily) {
    $grassFill=($tiles | Where-Object name -eq 'grass_interior_00' | Select-Object -First 1).image
    $otherFill=($tiles | Where-Object name -eq ($otherFamily+'_interior_00') | Select-Object -First 1).image
    $start = 0.0; $end = 0.0
    foreach($axis in 0,1,2,3) { $start += (Get-Anchor $image $direction $axis)/4.0 }
    foreach($axis in 60,61,62,63) { $end += (Get-Anchor $image $direction $axis)/4.0 }
    $aligned=New-Bitmap 64 64
    for($axis=0;$axis -lt 64;$axis++) {
        $shift=32-($start+($end-$start)*$axis/63.0)
        for($p=0;$p -lt 64;$p++) {
            $sample=[int][Math]::Round($p-$shift)
            if($sample -lt 0 -or $sample -gt 63) {
                $grassAtStart=($direction -eq 'above' -or $direction -eq 'left')
                $useGrass=($sample -lt 0 -and $grassAtStart) -or ($sample -gt 63 -and -not $grassAtStart)
                $fill=if($useGrass) { $grassFill } else { $otherFill }
                if($direction -eq 'above' -or $direction -eq 'below') { $color=$fill.GetPixel($axis,$p) } else { $color=$fill.GetPixel($p,$axis) }
            } elseif($direction -eq 'above' -or $direction -eq 'below') { $color=$image.GetPixel($axis,$sample) }
            else { $color=$image.GetPixel($sample,$axis) }
            if($direction -eq 'above' -or $direction -eq 'below') { $aligned.SetPixel($axis,$p,$color) }
            else { $aligned.SetPixel($p,$axis,$color) }
        }
    }
    $image.Dispose()
    return $aligned
}

# Crop only individual accepted swatches, excluding their presentation frames.
$groundFile='terrain-v06-ground-variants.png'
$groundXs=@(@(25,145,266,386),@(534,655,775,895),@(1043,1165,1285,1406))
$groundYs=@(83,208,333,457)
$groundFamilies=@('grass','dirt','water')
for($f=0;$f -lt 3;$f++) {
    for($r=0;$r -lt 4;$r++) { for($c=0;$c -lt 4;$c++) {
        $x=$groundXs[$f][$c]+2; $y=$groundYs[$r]+2
        $image=Get-Crop $groundFile $x $y 102 105
        $number=$r*4+$c
        Add-Tile ('{0}_interior_{1:d2}' -f $groundFamilies[$f],$number) $groundFamilies[$f] $image @{file=$groundFile;x=$x;y=$y;width=102;height=105}
    } }
}
$stoneFile='terrain-v06-stone-variants.png'
$stoneXs=@(40,214,389,560,733,908); $stoneYs=@(81,233,384,536)
for($r=0;$r -lt 4;$r++) { for($c=0;$c -lt 6;$c++) {
    $x=$stoneXs[$c]+2; $y=$stoneYs[$r]+2
    $image=Get-Crop $stoneFile $x $y 135 129
    Add-Tile ('stone_interior_{0:d2}' -f ($r*6+$c)) 'stone' $image @{file=$stoneFile;x=$x;y=$y;width=135;height=129}
} }

# Standardize a thin perimeter for full material variants. The interior art is preserved.
# Each family shares the same border colors and no navy presentation pixels.
$baseColors=@{}
foreach($family in @('grass','dirt','water','stone')) {
    $base=($tiles | Where-Object family -eq $family | Select-Object -First 1).image
    $red=0L; $green=0L; $blue=0L; $count=0
    for($y=8;$y -lt 56;$y+=3) { for($x=8;$x -lt 56;$x+=3) {
        $pixel=$base.GetPixel($x,$y); $red+=$pixel.R; $green+=$pixel.G; $blue+=$pixel.B; $count++
    } }
    $baseColors[$family]=[System.Drawing.Color]::FromArgb(255,[int]($red/$count),[int]($green/$count),[int]($blue/$count))
    if($family -eq 'stone') { continue }
    foreach($tile in ($tiles | Where-Object family -eq $family)) {
        for($y=0;$y -lt 64;$y++) { for($x=0;$x -lt 64;$x++) {
            $distance=[Math]::Min([Math]::Min($x,63-$x),[Math]::Min($y,63-$y))
            if($distance -lt 3) {
                $weight=@(1.0,0.66,0.33)[$distance]
                $tile.image.SetPixel($x,$y,(Mix-Color $tile.image.GetPixel($x,$y) $baseColors[$family] $weight))
            }
        } }
        $tile.notes='Three-pixel perimeter transitions to the common family base; nearest-neighbor crop preserved inside.'
    }
}

$profiles=@('straight','small_out','large_out','small_in','large_in','s_edge')
$dirs=@('above','below','left','right')
$dirtExtra=@('outer_nw','outer_ne','outer_sw','outer_se','grass_center','dirt_center','inner_nw','inner_ne','inner_sw','inner_se','rounded_extra_a','rounded_extra_b','gentle_nw','gentle_ne','gentle_sw','gentle_se','grass_connector','dirt_connector','grass_end_n','grass_end_s','grass_end_e','grass_end_w','grass_island','dirt_island')
$waterExtra=@('outer_nw','outer_ne','outer_sw','outer_se','water_center','grass_center','inner_nw','inner_ne','inner_sw','inner_se','near_curve_a','near_curve_b','transition_00','transition_01','transition_02','transition_03','transition_04','transition_05','channel','land_tongue','bay','peninsula','pond','near_s_extra')
$boundarySets=@(
    @{file='terrain-v05-dirt-boundaries.png'; family='dirt_boundary'; xs=@(90,244,410,579,744,915); ys=@(80,203,321,453,594,727,854,984); widths=@(136,147,149,145,152,148); heights=@(107,105,119,124,116,112,115,122); extra=$dirtExtra},
    @{file='terrain-v05-water-boundaries.png'; family='water_boundary'; xs=@(106,265,428,597,760,928); ys=@(85,205,322,443,565,687,810,932); widths=@(135,139,147,141,145,139); heights=@(106,104,108,108,109,109,111,120); extra=$waterExtra}
)
foreach($set in $boundarySets) {
    for($r=0;$r -lt 8;$r++) { for($c=0;$c -lt 6;$c++) {
        $x=$set.xs[$c]+2; $y=$set.ys[$r]+2; $w=$set.widths[$c]-4; $h=$set.heights[$r]-4
        $image=Get-Crop $set.file $x $y $w $h
        $notes='Accepted source swatch cropped individually; corner and feature seam compatibility not universally normalized.'
        if($r -lt 4) {
            $name='{0}_land_{1}_{2}' -f $set.family,$dirs[$r],$profiles[$c]
            $otherFamily=if($set.family -eq 'water_boundary') { 'water' } else { 'dirt' }
            $image=Align-Boundary $image $dirs[$r] $otherFamily
            $notes='Boundary endpoint averages re-anchored to pixel32 at opposite edges; original curvature and directional stone faces retained.'
        } else { $name='{0}_{1}' -f $set.family,$set.extra[($r-4)*6+$c] }
        Add-Tile $name $set.family $image @{file=$set.file;x=$x;y=$y;width=$w;height=$h} $notes
    } }
}

# Construct missing regular stone/grass and stone/dirt modules from approved fills.
# The same 16px outside margin and 5px segmented low curb are shared by straight and corner pieces.
function Test-StoneMask([string]$kind,[int]$x,[int]$y) {
    switch($kind) {
        'edge_n' { return $y -ge 16 }; 'edge_s' { return $y -lt 48 }
        'edge_w' { return $x -ge 16 }; 'edge_e' { return $x -lt 48 }
        'outer_nw' { return ($x -ge 16 -and $y -ge 16) }; 'outer_ne' { return ($x -lt 48 -and $y -ge 16) }
        'outer_sw' { return ($x -ge 16 -and $y -lt 48) }; 'outer_se' { return ($x -lt 48 -and $y -lt 48) }
        'inner_nw' { return (-not ($x -lt 16 -and $y -lt 16)) }; 'inner_ne' { return (-not ($x -ge 48 -and $y -lt 16)) }
        'inner_sw' { return (-not ($x -lt 16 -and $y -ge 48)) }; 'inner_se' { return (-not ($x -ge 48 -and $y -ge 48)) }
    }
}
$stoneBase=($tiles | Where-Object name -eq 'stone_interior_00').image
$stoneKinds=@('edge_n','edge_s','edge_w','edge_e','outer_nw','outer_ne','outer_sw','outer_se','inner_nw','inner_ne','inner_sw','inner_se')
$stoneMasks=@{}; $curbDistances=@{}; $curbSides=@{}
foreach($kind in $stoneKinds) {
    $mask=[bool[,]]::new(74,74)
    for($my=-5;$my -le 68;$my++) { for($mx=-5;$mx -le 68;$mx++) { $mask[($mx+5),($my+5)]=Test-StoneMask $kind $mx $my } }
    $distances=[byte[,]]::new(64,64); $sides=[char[,]]::new(64,64)
    for($y=0;$y -lt 64;$y++) { for($x=0;$x -lt 64;$x++) {
        $distances[$x,$y]=6
        if(-not $mask[($x+5),($y+5)]) { continue }
        foreach($d in 1,2,3,4,5) {
            if(-not $mask[($x+5),($y+5-$d)]) { $distances[$x,$y]=$d; $sides[$x,$y]='n'; break }
            if(-not $mask[($x+5),($y+5+$d)]) { $distances[$x,$y]=$d; $sides[$x,$y]='s'; break }
            if(-not $mask[($x+5-$d),($y+5)]) { $distances[$x,$y]=$d; $sides[$x,$y]='w'; break }
            if(-not $mask[($x+5+$d),($y+5)]) { $distances[$x,$y]=$d; $sides[$x,$y]='e'; break }
        }
    } }
    $stoneMasks[$kind]=$mask; $curbDistances[$kind]=$distances; $curbSides[$kind]=$sides
}
Write-Output 'Accepted swatches extracted; shared stone curb maps prepared.'
foreach($outsideFamily in @('grass','dirt')) {
    $outsideBase=($tiles | Where-Object name -eq ($outsideFamily+'_interior_00')).image
    foreach($kind in $stoneKinds) {
        $image=New-Bitmap 64 64
        $mask=$stoneMasks[$kind]; $distances=$curbDistances[$kind]; $sides=$curbSides[$kind]
        for($y=0;$y -lt 64;$y++) { for($x=0;$x -lt 64;$x++) {
            $inside=$mask[($x+5),($y+5)]
            if($inside) {
                $color=$stoneBase.GetPixel($x,$y)
                $distance=$distances[$x,$y]; $curbSide=$sides[$x,$y]
                if($distance -le 5) {
                    $tangent=if($curbSide -eq 'n' -or $curbSide -eq 's') { $x } else { $y }
                    $phase=(($tangent-16)%8+8)%8
                    $color=[System.Drawing.Color]::FromArgb(255,177,174,155)
                    if($distance -eq 1 -or $distance -eq 5) { $color=[System.Drawing.Color]::FromArgb(255,135,137,123) }
                    elseif($distance -eq 2) { $color=[System.Drawing.Color]::FromArgb(255,200,194,172) }
                    if($phase -eq 0) { $color=[System.Drawing.Color]::FromArgb(255,133,136,123) }
                    elseif($phase -eq 1 -and $distance -gt 1 -and $distance -lt 5) { $color=[System.Drawing.Color]::FromArgb(255,188,185,164) }
                }
            } else { $color=$outsideBase.GetPixel($x,$y) }
            $image.SetPixel($x,$y,$color)
        } }
        Add-Tile ('stone_to_{0}_{1}' -f $outsideFamily,$kind) ('stone_to_'+$outsideFamily) $image @{derivedFrom=@('stone_interior_00',($outsideFamily+'_interior_00'));outsideMargin=16;curbWidth=5;curbBlockPitch=8} 'Shared exact mask anchors and segmented low curb. Concave modules are stone wrapping an outside-material corner cutout; no rotated cast shadows.'
    }
}

function Save-Montage([string]$name,$entries,[int]$columns,[int]$scale=2) {
    $rows=[int][Math]::Ceiling($entries.Count/[double]$columns)
    $out=New-Bitmap ($columns*64*$scale) ($rows*64*$scale)
    $g=[System.Drawing.Graphics]::FromImage($out); $g.Clear([System.Drawing.Color]::FromArgb(255,20,30,42))
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor; $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    for($i=0;$i -lt $entries.Count;$i++) { $g.DrawImage($entries[$i].image,[System.Drawing.Rectangle]::new(($i%$columns)*64*$scale,[int][Math]::Floor($i/$columns)*64*$scale,64*$scale,64*$scale),0,0,64,64,[System.Drawing.GraphicsUnit]::Pixel) }
    $g.Dispose(); $out.Save((Join-Path $PSScriptRoot $name),[System.Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
}
function Find-Tile([string]$name) { return ($tiles | Where-Object name -eq $name | Select-Object -First 1) }
$cropProof=@()
foreach($family in @('grass','dirt','water','stone')) { foreach($n in 0,3,7,10,12,15) { $cropProof += Find-Tile ('{0}_interior_{1:d2}' -f $family,$n) } }
Save-Montage 'crop-proof.png' $cropProof 6 2
$stoneProof=@()
foreach($family in @('grass','dirt')) {
    $prefix='stone_to_'+$family+'_'
    $stoneProof += @( (Find-Tile ($prefix+'outer_nw')), (Find-Tile ($prefix+'edge_n')), (Find-Tile ($prefix+'outer_ne')), (Find-Tile ($prefix+'outer_nw')), (Find-Tile ($prefix+'edge_n')), (Find-Tile ($prefix+'outer_ne')) )
    $stoneProof += @( (Find-Tile ($prefix+'edge_w')), (Find-Tile 'stone_interior_00'), (Find-Tile ($prefix+'edge_e')), (Find-Tile ($prefix+'edge_w')), (Find-Tile ($prefix+'inner_se')), (Find-Tile ($prefix+'outer_se')) )
    $stoneProof += @( (Find-Tile ($prefix+'outer_sw')), (Find-Tile ($prefix+'edge_s')), (Find-Tile ($prefix+'outer_se')), (Find-Tile ($prefix+'outer_sw')), (Find-Tile ($prefix+'outer_se')), (Find-Tile ($family+'_interior_00')) )
}
Save-Montage 'stone-courtyard-proof.png' $stoneProof 6 2
$waterProof=@()
foreach($side in @('above','below')) { foreach($profile in $profiles) { $waterProof+=Find-Tile ('water_boundary_land_{0}_{1}' -f $side,$profile) } }
Save-Montage 'water-direction-proof.png' $waterProof 6 2

if($Pack) {
    $columns=16; $rows=[int][Math]::Ceiling($tiles.Count/16.0)
    $atlas=New-Bitmap ($columns*64) ($rows*64)
    $g=[System.Drawing.Graphics]::FromImage($atlas); $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $records=@()
    for($i=0;$i -lt $tiles.Count;$i++) {
        $column=$i%16; $row=[int][Math]::Floor($i/16.0)
        $g.DrawImageUnscaled($tiles[$i].image,$column*64,$row*64)
        $records += [ordered]@{index=$i;name=$tiles[$i].name;family=$tiles[$i].family;column=$column;rowTop=$row;x=$column*64;y=($rows-1-$row)*64;width=64;height=64;source=$tiles[$i].source;notes=$tiles[$i].notes}
    }
    $g.Dispose(); $atlas.Save((Join-Path $PSScriptRoot 'terrain-tileset-64.png'),[System.Drawing.Imaging.ImageFormat]::Png); $atlas.Dispose()
    $manifest=[ordered]@{schemaVersion=1;image='terrain-tileset-64.png';width=$columns*64;height=$rows*64;cellSize=64;columns=16;rows=$rows;padding=0;margin=0;tileCount=$tiles.Count;unusedTransparentCells=$columns*$rows-$tiles.Count;pixelsPerUnit=64;filterMode='Point';compression='None';mipmaps=$false;coordinateSystem='x/y use Unity bottom-left, rowTop is top-down';tiles=$records;limitations=@('Exact64px slicing is verified by construction. Full artistic edge continuity is not universally guaranteed.','Dirt/water directional edge variants are re-anchored; general feature/corner swatches retain accepted source geometry.','Stone mask modules have exact shared geometry; cobble texture is reused from approved base, without a seamless texture synthesis claim.','PNG is staged only; no Unity importer or scene mutation was performed by this script.')}
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'terrain-tileset-64.json') -Encoding UTF8
}
foreach($source in $sources.Values) { $source.Dispose() }
foreach($tile in $tiles) { $tile.image.Dispose() }
[pscustomobject]@{tileCount=$tiles.Count;packed=[bool]$Pack;output=$PSScriptRoot} | ConvertTo-Json -Compress
