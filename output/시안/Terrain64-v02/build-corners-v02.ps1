$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$workDir='C:/Users/Merkatte/Desktop/QuickSort/NPC_WORK_PROJECT/output/시안/Terrain64-v02'
$oldDir='C:/Users/Merkatte/Desktop/QuickSort/NPC_WORK_PROJECT/output/시안/Terrain64'
$oldAtlas=[System.Drawing.Bitmap]::FromFile((Join-Path $oldDir 'terrain-tileset-64.png'))
$map=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $oldDir 'terrain-tileset-64.json') | ConvertFrom-Json
$atlas=[System.Drawing.Bitmap]::new(1024,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($atlas); $g.DrawImageUnscaled($oldAtlas,0,0); $g.Dispose()
$fills=@{}
foreach($entry in @(@('grass',0),@('dirt',16),@('water',32))) {
    $i=[int]$entry[1]
    $fills[$entry[0]]=$oldAtlas.Clone([System.Drawing.Rectangle]::new(($i%16)*64,([int][Math]::Floor($i/16))*64,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}
$baseGrass=$fills.grass.GetPixel(0,0); $baseDirt=$fills.dirt.GetPixel(0,0); $baseWater=$fills.water.GetPixel(0,0)
$rock=[System.Drawing.Color[,]]::new(64,9)
for($y=0;$y -lt 9;$y++) { for($x=0;$x -lt 64;$x++) {
    $p=$oldAtlas.GetPixel(512+$x,448+34+$y)
    if($p.G -gt $p.R*1.025 -and $p.G -gt $p.B*1.25) { $p=[System.Drawing.Color]::FromArgb(174,172,146) }
    elseif($p.R -lt 25 -and $p.B -gt $p.R*3) { $p=[System.Drawing.Color]::FromArgb(102,116,111) }
    $rock[$x,$y]=$p
} }
$sideRock=[System.Drawing.Color]::FromArgb(119,132,122)
$edgeGrass=[System.Drawing.Color]::FromArgb([int]($baseGrass.R*.97),[int]($baseGrass.G*.98),$baseGrass.B)
$ripple=[System.Drawing.Color]::FromArgb(42,137,157)
$masks=@{}; $edgeKinds=@{}
# Pixel silhouette offsets measured from the approved dirt fringe, never used at contact collars.
$fringe=@(0,1,0,0,1,3,1,-1,0,0,0,0,0,1,0,-1,0,2,-1,0,-3,-1,-1,0,0,0,-1,-1,0,0,0,-2,-3,-1,-1,-1,-1,0,0,0,-1,-1,-2,0,-1,0,0,0,-3,0,0,0,0,0,-1,-1,0,0,-1,-2,1,1,0,-1)
$profiles=@('straight','small_out','large_out','small_in','large_in','s_edge')
$directions=@('above','below','left','right')

# Binary geometry is computed once and shared by dirt and water rendering.
# All directional endpoints have an eight-pixel straight collar and cross at32.
foreach($direction in $directions) { foreach($profile in $profiles) {
    $key='land_'+$direction+'_'+$profile
    $mask=[bool[,]]::new(64,64)
    for($axis=0;$axis -lt 64;$axis++) {
        $t=[Math]::Max(0.0,[Math]::Min(1.0,($axis-8)/47.0))
        $hump=[Math]::Pow([Math]::Sin([Math]::PI*$t),2)
        $offset=switch($profile) {'small_out' {8*$hump} 'large_out' {16*$hump} 'small_in' {-8*$hump} 'large_in' {-16*$hump} 's_edge' {10*[Math]::Sin(2*[Math]::PI*$t)*[Math]::Sin([Math]::PI*$t)} default {0}}
        if($direction -eq 'below' -or $direction -eq 'right') { $offset=-$offset }
        $cut=32+[int][Math]::Round($offset)
        for($p=0;$p -lt 64;$p++) {
            $inside=if($direction -eq 'above' -or $direction -eq 'left') {$p -lt $cut} else {$p -ge $cut}
            if($direction -eq 'above' -or $direction -eq 'below') {$mask[$axis,$p]=$inside} else {$mask[$p,$axis]=$inside}
        }
    }
    $masks[$key]=$mask
} }
foreach($kind in @('outer','inner')) { foreach($quadrant in @('nw','ne','sw','se')) {
    $mask=[bool[,]]::new(64,64)
    for($y=0;$y -lt 64;$y++) { for($x=0;$x -lt 64;$x++) {
        # A quarter disk connects two ADJACENT edges, never opposite edges or a U-notch.
        $u=[Math]::Max(0.0,[Math]::Min(64.0,($x-3)*64.0/57.0))
        $v=[Math]::Max(0.0,[Math]::Min(64.0,($y-3)*64.0/57.0))
        $dx=if($quadrant.EndsWith('w')) {$u} else {64-$u}
        $dy=if($quadrant.StartsWith('n')) {$v} else {64-$v}
        $quarter=($dx*$dx+$dy*$dy -lt 1024)
        $mask[$x,$y]=if($kind -eq 'outer') {$quarter} else {-not $quarter}
    } }
    $masks[$kind+'_'+$quadrant]=$mask
} }

function Get-EdgeKind($mask,[string]$edge) {
    $first=switch($edge){'n' {$mask[0,0]} 's' {$mask[0,63]} 'w' {$mask[0,0]} 'e' {$mask[63,0]}}
    $last=switch($edge){'n' {$mask[63,0]} 's' {$mask[63,63]} 'w' {$mask[0,63]} 'e' {$mask[63,63]}}
    if($first -and $last) {return 'grass'}
    if(-not $first -and -not $last) {return 'other'}
    if($first) {return 'grass_first32'}
    return 'grass_last32'
}
function Edge-Color([string]$kind,[string]$edge,[int]$position,[string]$family) {
    $other=if($family -eq 'water') {$baseWater} else {$baseDirt}
    if($kind -eq 'grass') {return $baseGrass}
    if($kind -eq 'other') {return $other}
    $inside=if($kind -eq 'grass_first32') {$position -lt 32} else {$position -ge 32}
    if($inside) {
        if(($kind -eq 'grass_first32' -and $position -eq 31) -or ($kind -eq 'grass_last32' -and $position -eq 32)) {return $edgeGrass}
        return $baseGrass
    }
    if($family -eq 'water') {
        if(($edge -eq 'w' -or $edge -eq 'e') -and $kind -eq 'grass_first32' -and $position -ge 32 -and $position -lt 41) {return $rock[0,($position-32)]}
        if(($edge -eq 'w' -or $edge -eq 'e') -and $kind -eq 'grass_first32' -and $position -eq 41) {return [System.Drawing.Color]::FromArgb(19,112,151)}
        if(($edge -eq 'n' -or $edge -eq 's') -and (($kind -eq 'grass_first32' -and $position -eq 32) -or ($kind -eq 'grass_last32' -and $position -eq 31))) {return $sideRock}
    }
    return $other
}

$rendered=@{}
foreach($family in @('dirt','water')) {
    foreach($key in $masks.Keys) {
        $canonical=$masks[$key]
        $mask=[bool[,]]::new(64,64)
        for($my=0;$my -lt 64;$my++) {for($mx=0;$mx -lt 64;$mx++) {
            $collar=[Math]::Min([Math]::Min($mx,63-$mx),[Math]::Min($my,63-$my))
            $scale=[Math]::Min(1.0,[Math]::Max(0.0,($collar-3)/4.0))
            $sx=[Math]::Max(0,[Math]::Min(63,$mx+[int][Math]::Round($fringe[$my]*$scale)))
            $sy=[Math]::Max(0,[Math]::Min(63,$my+[int][Math]::Round($fringe[$mx]*$scale)))
            $mask[$mx,$my]=if($family -eq 'water') {$canonical[$mx,$my] -or $canonical[$sx,$sy]} else {$canonical[$sx,$sy]}
        }}
        $image=[System.Drawing.Bitmap]::new(64,64,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $edges=@{}; foreach($edge in @('n','s','w','e')) {$edges[$edge]=Get-EdgeKind $mask $edge}
        for($y=0;$y -lt 64;$y++) { for($x=0;$x -lt 64;$x++) {
            $inside=$mask[$x,$y]
            if($inside) {
                $color=$fills.grass.GetPixel($x,$y)
                $nearOther=($x -gt 0 -and -not $mask[($x-1),$y]) -or ($x -lt 63 -and -not $mask[($x+1),$y]) -or ($y -gt 0 -and -not $mask[$x,($y-1)]) -or ($y -lt 63 -and -not $mask[$x,($y+1)])
                if($nearOther) {
                    # Thin in-mask grass lip; it does not move the shared geometric boundary.
                    $phase=($x*3+$y*5)%7
                    $color=if($phase -lt 2) {[System.Drawing.Color]::FromArgb(162,174,55)} else {$edgeGrass}
                }
            } else {
                $color=$fills[$family].GetPixel($x,$y)
                if($family -eq 'water') {
                    $depth=0
                    for($d=1;$d -le 10;$d++) {
                        $above=[Math]::Max(0,$y-$d)
                        if($canonical[$x,$above]) {$depth=$d;break}
                    }
                    if($depth -gt 0 -and $depth -le 9) {$color=$rock[$x,($depth-1)]}
                    elseif($depth -eq 10) {$color=[System.Drawing.Color]::FromArgb(19,112,151)}
                    else {
                        $left=($x -gt 0 -and $canonical[($x-1),$y]); $right=($x -lt 63 -and $canonical[($x+1),$y])
                        $below=($y -lt 63 -and $canonical[$x,($y+1)])
                        if(($left -or $right) -and -not $below) {$color=$sideRock}
                    }
                }
            }
            $image.SetPixel($x,$y,$color)
        } }
        # One-pixel contact signatures are identical to adjoining canonical straight tiles.
        # Existing base fills already share the same thin periodic outer perimeter.
        for($p=0;$p -lt 64;$p++) {
            $image.SetPixel($p,0,(Edge-Color $edges.n 'n' $p $family))
            $image.SetPixel($p,63,(Edge-Color $edges.s 's' $p $family))
            $image.SetPixel(0,$p,(Edge-Color $edges.w 'w' $p $family))
            $image.SetPixel(63,$p,(Edge-Color $edges.e 'e' $p $family))
        }
        $rendered[$family+'_'+$key]=$image
        $edgeKinds[$family+'_'+$key]=$edges
    }
    Write-Output ($family+' geometry rendered.')
}

$changed=@()
foreach($family in @('dirt','water')) {
    $start=if($family -eq 'dirt'){72}else{120}
    for($d=0;$d -lt 4;$d++) {for($p=0;$p -lt 6;$p++) {
        $index=$start+$d*6+$p; $key='land_'+$directions[$d]+'_'+$profiles[$p]
        $changed+=@{index=$index;family=$family;key=$key}
    }}
    $outerStart=$start+24; $innerStart=$start+30
    for($q=0;$q -lt 4;$q++) {
        $quadrant=@('nw','ne','sw','se')[$q]
        $changed+=@{index=$outerStart+$q;family=$family;key='outer_'+$quadrant}
        $changed+=@{index=$innerStart+$q;family=$family;key='inner_'+$quadrant}
    }
}
$g=[System.Drawing.Graphics]::FromImage($atlas); $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
foreach($change in $changed) {
    $tile=$map.tiles[$change.index]; $renderKey=$change.family+'_'+$change.key
    $g.DrawImageUnscaled($rendered[$renderKey],[int]$tile.x,768-[int]$tile.y-64)
    $tile.notes='v02 canonical geometry: contacts at32; outer suffix is GRASS quadrant; inner suffix is OUTSIDE-MATERIAL CUTOUT quadrant. Fixed bottom-view water shading.'
    $tile.source=[pscustomobject]@{derivedFrom=@('grass_interior_00',($change.family+'_interior_00'));geometry=$change.key;anchor=32;rockSource=if($change.family -eq 'water'){'accepted water far-bank strip, camera-aware reconstruction'}else{$null}}
    $tile | Add-Member -NotePropertyName canonicalEdges -NotePropertyValue $edgeKinds[$renderKey] -Force
}
$g.Dispose()
foreach($tile in $map.tiles) {
    if($tile.family -in @('dirt_boundary','water_boundary') -and $tile.index -notin @($changed | ForEach-Object index)) {
        $tile.notes='Retained decorative/source feature. Not part of the canonical straight/corner interoperability set.'
        $tile.name=$tile.name -replace 'boundary_(rounded|gentle|near_curve|transition)','boundary_feature_$1'
    }
}
$map.image='terrain-tileset-64-v02.png'
$map | Add-Member -NotePropertyName version -NotePropertyValue 2 -Force
$map | Add-Member -NotePropertyName cornerDirectionMeaning -NotePropertyValue 'outer_NW/NE/SW/SE = convex GRASS quarter in named quadrant; inner_NW/NE/SW/SE = other-material concave CUTOUT in named quadrant, grass occupies remainder. Stone modules unchanged and use their prior perimeter-placement convention.' -Force
$map | Add-Member -NotePropertyName rebuiltIndices -NotePropertyValue @($changed | ForEach-Object index | Sort-Object) -Force
$map.limitations=@('Canonical dirt/water straight and corner contact geometry is rebuilt at32. All old source features outside this core remain decorative and are not guaranteed interoperable.','Approved interior texture variation and repeated cobble patterns are unchanged; geometric compatibility does not imply every texture detail is seamless.','Water rock faces are reconstructed from approved bank pixels with fixed bottom-view occlusion; no rotation of final bank artwork.','Output only; no Unity import, metadata or production asset changes.')
$atlas.Save((Join-Path $workDir 'terrain-tileset-64-v02.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$map | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath (Join-Path $workDir 'terrain-tileset-64-v02.json') -Encoding UTF8

function Get-TileImage([int]$index) {return $atlas.Clone([System.Drawing.Rectangle]::new(($index%16)*64,([int][Math]::Floor($index/16))*64,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
function Index-For([string]$family,[string]$key) {
    if($key -eq 'grass') {return 0}
    if($key -eq 'other') {if($family -eq 'dirt'){return 16}else{return 32}}
    return ($changed | Where-Object {$_.family -eq $family -and $_.key -eq $key} | Select-Object -First 1).index
}
$pool=@('inner_se','land_above_straight','inner_sw','land_left_straight','other','land_right_straight','inner_ne','land_below_straight','inner_nw')
$island=@('outer_se','land_below_straight','outer_sw','land_right_straight','grass','land_left_straight','outer_ne','land_above_straight','outer_nw')
$mixed=@('inner_se','land_above_small_out','land_above_large_in','inner_sw','land_left_small_in','other','other','land_right_small_out','land_left_large_out','other','other','land_right_large_in','inner_ne','land_below_small_out','land_below_s_edge','inner_nw')
$font=[System.Drawing.Font]::new('Consolas',13)
foreach($family in @('dirt','water')) {
    $proof=[System.Drawing.Bitmap]::new(768,976); $pg=[System.Drawing.Graphics]::FromImage($proof); $pg.Clear([System.Drawing.Color]::FromArgb(19,28,39))
    $pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor; $pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $pg.DrawString('INNER CUTOUTS: '+$family+' pool',$font,[System.Drawing.Brushes]::White,0,0)
    $pg.DrawString('OUTER: grass island',$font,[System.Drawing.Brushes]::White,384,0)
    for($set=0;$set -lt 2;$set++) {
        $keys=if($set -eq 0){$pool}else{$island}
        for($i=0;$i -lt 9;$i++) {
            $index=Index-For $family $keys[$i]; $tile=Get-TileImage $index
            $pg.DrawImage($tile,[System.Drawing.Rectangle]::new($set*384+($i%3)*128,28+([int][Math]::Floor($i/3))*128,128,128)); $tile.Dispose()
        }
    }
    $pg.DrawString('MIXED: bulge / recess / S + corners',$font,[System.Drawing.Brushes]::White,0,430)
    for($i=0;$i -lt 16;$i++) {
        $index=Index-For $family $mixed[$i]; $tile=Get-TileImage $index
        $pg.DrawImage($tile,[System.Drawing.Rectangle]::new(($i%4)*128,458+([int][Math]::Floor($i/4))*128,128,128)); $tile.Dispose()
    }
    $pg.Dispose(); $proof.Save((Join-Path $workDir ($family+'-connections-proof.png')),[System.Drawing.Imaging.ImageFormat]::Png); $proof.Dispose()
}
$font.Dispose(); $atlas.Dispose(); $oldAtlas.Dispose()
foreach($im in $fills.Values){$im.Dispose()}; foreach($im in $rendered.Values){$im.Dispose()}
Write-Output 'v02 staged:1024x768,192cells,64core cells rebuilt; production untouched.'
