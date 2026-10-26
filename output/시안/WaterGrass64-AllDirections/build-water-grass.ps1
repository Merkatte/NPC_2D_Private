$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'output/시안/WaterGrass64-AllDirections'
$old=[System.Drawing.Bitmap]::FromFile((Join-Path (Get-Location) 'Assets/Art/Generated/TerrainTest/terrain-tileset-64.png'))
$repaired=[System.Drawing.Bitmap]::FromFile((Join-Path (Get-Location) 'output/시안/Terrain64-v03/terrain-tileset-64-v03.png'))
$oldMap=Get-Content -Raw -Encoding UTF8 -LiteralPath 'output/시안/Terrain64/terrain-tileset-64.json'|ConvertFrom-Json
$sourcePaths=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $dir 'generated-sources.json')|ConvertFrom-Json
function Crop-Tile($sheet,[int]$index){return $sheet.Clone([System.Drawing.Rectangle]::new(($index%16)*64,([int][Math]::Floor($index/16))*64,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
$water=Crop-Tile $old 32
$grassFill=Crop-Tile $old 149
$images=@{};$records=@();$rearCount=0;$waterCount=0
$cardinal=@('above','below','left','right');$cw=@(3,2,0,1)
$physical=@('NW','NE','SE','SW')
$island=@{NW=147;NE=146;SE=144;SW=145};$pool=@{NW=150;NE=151;SE=153;SW=152}
$corners=@{144='SE';145='SW';146='NE';147='NW';150='NW';151='NE';152='SW';153='SE'}
foreach($family in 120..167){
 $strip=$null
 if($family -ge 154){
  $path=$sourcePaths.PSObject.Properties[[string]$family].Value
  Copy-Item -LiteralPath $path -Destination (Join-Path $dir ('generated-'+$family+'.png')) -Force
  $strip=[System.Drawing.Bitmap]::FromFile($path)
 }
 foreach($q in 0..3){
  $slot=($family-120)*4+$q;$sourceIndex=$family;$mode='original_world_oriented';$placement=$null;$bankCleaned=0;$waterReplaced=0
  if($family -lt 144){
   $d=[int][Math]::Floor(($family-120)/6);$p=($family-120)%6
   for($turn=0;$turn -lt $q;$turn++){$d=$cw[$d]}
   $sourceIndex=120+$d*6+$p;$b=Crop-Tile $old $sourceIndex;$placement='land_'+$cardinal[$d]
  }elseif($corners.ContainsKey($family)){
   $p=[Array]::IndexOf($physical,$corners[$family]);$placement=$physical[($p+$q)%4]
   if($family -lt 150){$sourceIndex=$island[$placement];$placement='island_'+$placement}else{$sourceIndex=$pool[$placement];$placement='pool_'+$placement}
   $b=Crop-Tile $repaired $sourceIndex;$mode='v03_world_oriented_corner'
  }elseif($family -lt 154 -or $q -eq 0){$b=Crop-Tile $old $family}
  else{
   $mode='new_world_oriented_special';$sourceIndex=$null
   $b=[System.Drawing.Bitmap]::new(64,64,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[System.Drawing.Graphics]::FromImage($b);$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
   $drawSource=$strip;$fixImage=$null
   if($q -eq 2 -and $family -in @(158,161)){$fixPath=if($family -eq 158){'C:\Users\Merkatte\.codex\generated_images\01a17248-a290-7c62-8413-fe5db02e5aac\exec-281d6c95-9219-4e5e-9614-b1102ce0a81c.png'}else{'C:\Users\Merkatte\.codex\generated_images\01a17248-a290-7c62-8413-fe5db02e5aac\exec-f869d64a-8d11-43e5-99c8-248a720f9898.png'};$fixImage=[System.Drawing.Bitmap]::FromFile($fixPath);$drawSource=$fixImage}
   $left=[int][Math]::Round(($q-1)*$drawSource.Width/3);$right=[int][Math]::Round($q*$drawSource.Width/3)
   $g.DrawImage($drawSource,[System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Rectangle]::new($left,0,$right-$left,$drawSource.Height),[System.Drawing.GraphicsUnit]::Pixel);$g.Dispose();if($fixImage){$fixImage.Dispose()}
   $grass=[bool[,]]::new(64,64);$up=[int[,]]::new(64,64);$down=[int[,]]::new(64,64)
   for($x=0;$x -lt 64;$x++){
    $last=-100
    for($y=0;$y -lt 64;$y++){$c=$b.GetPixel($x,$y);$grass[$x,$y]=$c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.5 -and $c.G -gt 45;$up[$x,$y]=$y-$last;if($grass[$x,$y]){$last=$y}}
    $last=164
    for($y=63;$y -ge 0;$y--){$down[$x,$y]=$last-$y;if($grass[$x,$y]){$last=$y}}
   }
   # Rear-facing exposed rocks are removed only where land is below, retaining original yellow grass lip.
   for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
    $c=$b.GetPixel($x,$y)
    if(-not $grass[$x,$y] -and $down[$x,$y] -le 10 -and $up[$x,$y] -gt 10){
     if($down[$x,$y] -gt 1 -or $c.B -lt $c.R*1.5){$b.SetPixel($x,$y,$water.GetPixel($x,$y));$bankCleaned++}
    }
   }}
   # A fixed, unrotated approved water interior supplies horizontal ripple marks.
   $distance=[int[,]]::new(64,64)
   for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){$c=$b.GetPixel($x,$y);$isWater=$c.B -gt $c.R*1.5 -and $c.B -gt $c.G*1.05;$distance[$x,$y]=if($isWater){100}else{0}}}
   for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){if($x -gt 0){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[($x-1),$y]+1))};if($y -gt 0){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[$x,($y-1)]+1))}}}
   for($y=63;$y -ge 0;$y--){for($x=63;$x -ge 0;$x--){if($x -lt 63){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[($x+1),$y]+1))};if($y -lt 63){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[$x,($y+1)]+1))}}}
   for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
    if($distance[$x,$y] -ge 4){$b.SetPixel($x,$y,$water.GetPixel($x,$y));$waterReplaced++}
    elseif($distance[$x,$y] -gt 0){$c=$b.GetPixel($x,$y);$b.SetPixel($x,$y,[System.Drawing.Color]::FromArgb([Math]::Max(0,$c.R-2),[Math]::Min(255,$c.G+7),[Math]::Max(0,$c.B-3)))}
   }}
   # Remove generator separator contamination only at an edge whose interior is overwhelmingly grass.
   $cleanSource=$b.Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
   foreach($edge in @('N','S','W','E')){
    $landCount=0
    for($p=0;$p -lt 64;$p++){$xx=if($edge -eq 'W'){7}elseif($edge -eq 'E'){56}else{$p};$yy=if($edge -eq 'N'){7}elseif($edge -eq 'S'){56}else{$p};$c=$cleanSource.GetPixel($xx,$yy);if($c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.5){$landCount++}}
    if($landCount -ge 52){
     for($p=0;$p -lt 64;$p++){for($d=0;$d -lt 6;$d++){
      $xx=if($edge -eq 'W'){12+$d}elseif($edge -eq 'E'){51-$d}else{$p};$yy=if($edge -eq 'N'){12+$d}elseif($edge -eq 'S'){51-$d}else{$p};$c=$cleanSource.GetPixel($xx,$yy)
      if($c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.5){$tx=if($edge -eq 'W'){$d}elseif($edge -eq 'E'){63-$d}else{$p};$ty=if($edge -eq 'N'){$d}elseif($edge -eq 'S'){63-$d}else{$p};$b.SetPixel($tx,$ty,$c)}
     }}
    }
   }
   # A divider from an adjacent generated cell can falsely close a water opening at the outermost edge.
   foreach($edge in @('N','S','W','E')){for($p=0;$p -lt 64;$p++){
    $waterInside=$true
    foreach($depth in @(3,4,5)){$xx=if($edge -eq 'W'){$depth}elseif($edge -eq 'E'){63-$depth}else{$p};$yy=if($edge -eq 'N'){$depth}elseif($edge -eq 'S'){63-$depth}else{$p};$c=$cleanSource.GetPixel($xx,$yy);if(-not($c.B -gt $c.R*1.5 -and $c.B -gt $c.G*1.05)){$waterInside=$false}}
    if($waterInside){foreach($depth in @(0,1)){$xx=if($edge -eq 'W'){$depth}elseif($edge -eq 'E'){63-$depth}else{$p};$yy=if($edge -eq 'N'){$depth}elseif($edge -eq 'S'){63-$depth}else{$p};$c=$cleanSource.GetPixel($xx,$yy);if($c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.5){$b.SetPixel($xx,$yy,$water.GetPixel($xx,$yy))}}}
   }}
   $cleanSource.Dispose()
   $rearCount+=$bankCleaned;$waterCount+=$waterReplaced
  }
  $images[$slot]=$b
  $records+=[pscustomobject]@{index=$slot;familySourceIndex=$family;familyName=$oldMap.tiles[$family].name;shapeOrientationClockwise=$q*90;physicalPlacement=$placement;rowTop=[int][Math]::Floor($slot/16);column=$slot%16;x=($slot%16)*64;y=768-([int][Math]::Floor($slot/16)+1)*64;width=64;height=64;artMode=$mode;copiedSourceIndex=$sourceIndex;rearBankPixelsCleaned=$bankCleaned;fixedWaterPixels=$waterReplaced;connectionStatus=if($family -lt 154){'Established core equivalent; no bitmap rotation'}else{'Feature endpoints vary; inspect matching edge profiles, not universal autotile'}}
 }
 if($strip){$strip.Dispose()}
 Write-Output ('Family '+$family+' ready')
}

# The original F161 source includes two dark presentation-frame rows below a full-grass edge.
$frameFix=$images[(161-120)*4].Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for($x=0;$x -lt 64;$x++){foreach($y in @(62,63)){$images[(161-120)*4].SetPixel($x,$y,$frameFix.GetPixel($x,$y-2))}}
$frameFix.Dispose()
$featureOriginals=@{};foreach($slot in 136..191){$featureOriginals[$slot]=$images[$slot].Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
function Edge-Profile($im,[string]$edge){
 $flags=[bool[]]::new(64)
 for($p=0;$p -lt 64;$p++){$xx=if($edge -eq 'W'){0}elseif($edge -eq 'E'){63}else{$p};$yy=if($edge -eq 'N'){0}elseif($edge -eq 'S'){63}else{$p};$c=$im.GetPixel($xx,$yy);$flags[$p]=$c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.5}
 $smooth=[bool[]]::new(64);for($p=0;$p -lt 64;$p++){$yes=0;$count=0;for($k=[Math]::Max(0,$p-2);$k -le [Math]::Min(63,$p+2);$k++){if($flags[$k]){$yes++};$count++};$smooth[$p]=$yes -ge $count*.5}
 $anchors=@(0);for($p=1;$p -lt 64;$p++){if($smooth[$p] -ne $smooth[$p-1]){$anchors+=$p}};$anchors+=63
 return ,$anchors
}
$matchedBands=@();$unmatched=@()
function Fit-FeatureEdge([int]$slot,[string]$edge,$reference,[string]$refEdge,[string]$description){
 $im=$images[$slot];$source=$im.Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $from=Edge-Profile $source $edge;$to=Edge-Profile $reference $refEdge
 if($from.Count -ne $to.Count -or $from.Count -lt 3){$script:unmatched+=('slot'+$slot+' '+$edge+' '+$description);$source.Dispose();return}
 for($depth=0;$depth -lt 22;$depth++){for($p=0;$p -lt 64;$p++){
  $piece=0;while($piece -lt $to.Count-2 -and $p -gt $to[$piece+1]){$piece++}
  $length=[Math]::Max(1,$to[$piece+1]-$to[$piece]);$mapped=$from[$piece]+($p-$to[$piece])*($from[$piece+1]-$from[$piece])/$length
  $t=[Math]::Max(0.0,[Math]::Min(1.0,($depth-3)/18.0));$weight=1-$t*$t*(3-2*$t);$coord=[int][Math]::Round($p+($mapped-$p)*$weight);$coord=[Math]::Max(0,[Math]::Min(63,$coord))
  $sx=if($edge -eq 'W'){$depth}elseif($edge -eq 'E'){63-$depth}else{$coord};$sy=if($edge -eq 'N'){$depth}elseif($edge -eq 'S'){63-$depth}else{$coord}
  $tx=if($edge -eq 'W'){$depth}elseif($edge -eq 'E'){63-$depth}else{$p};$ty=if($edge -eq 'N'){$depth}elseif($edge -eq 'S'){63-$depth}else{$p}
  $color=$source.GetPixel($sx,$sy)
  if($depth -ge 4 -and $color.G -ge $color.R*.92 -and $color.G -gt $color.B*1.5){
   $deepGrass=$true
   foreach($delta in @(@(-5,0),@(5,0),@(0,-5),@(0,5))){
    $cx=[Math]::Max(0,[Math]::Min(63,$sx+$delta[0]));$cy=[Math]::Max(0,[Math]::Min(63,$sy+$delta[1]));$test=$source.GetPixel($cx,$cy)
    if(-not($test.G -ge $test.R*.92 -and $test.G -gt $test.B*1.5)){$deepGrass=$false;break}
   }
   if($deepGrass){$unwarped=$source.GetPixel($tx,$ty);$color=if($unwarped.G -ge $unwarped.R*.92 -and $unwarped.G -gt $unwarped.B*1.5){$unwarped}else{$grassFill.GetPixel($tx,$ty)}}
  }
  if($depth -lt 4){$rx=if($refEdge -eq 'W'){$depth}elseif($refEdge -eq 'E'){63-$depth}else{$p};$ry=if($refEdge -eq 'N'){$depth}elseif($refEdge -eq 'S'){63-$depth}else{$p};$color=$reference.GetPixel($rx,$ry)}
  $im.SetPixel($tx,$ty,$color)
 }}
 $records[$slot].connectionStatus='Matched4px endpoint bands: '+$description
 $script:matchedBands+=@{slot=$slot;edge=$edge;mate=$description}
 $source.Dispose()
}
foreach($family in @(156,157,158,160,161)){
 for($q=0;$q -lt 4;$q++){
  $d=if($family -eq 161){1}else{0};for($turn=0;$turn -lt $q;$turn++){$d=$cw[$d]}
  $refIndex=120+$d*6;$reference=Crop-Tile $old $refIndex;$slot=($family-120)*4+$q
  if($d -le 1){Fit-FeatureEdge $slot 'W' $reference 'E' ('v01 bank'+$refIndex);Fit-FeatureEdge $slot 'E' $reference 'W' ('v01 bank'+$refIndex)}
  else{Fit-FeatureEdge $slot 'N' $reference 'S' ('v01 bank'+$refIndex);Fit-FeatureEdge $slot 'S' $reference 'N' ('v01 bank'+$refIndex)}
  $reference.Dispose()
 }
}
# Matched opposite tongue ends form complete upright-land islands in the proof.
$base=(163-120)*4;$reference=$images[$base].Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
Fit-FeatureEdge ($base+2) 'N' $reference 'S' 'F163/0 south edge';$reference.Dispose()
$reference=$images[$base+1].Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
Fit-FeatureEdge ($base+3) 'E' $reference 'W' 'F163/90 west edge';$reference.Dispose()
# Channels share end profiles within horizontal/vertical sets.
$base=(162-120)*4
foreach($orientation in @('vertical','horizontal')){
 $template=if($orientation -eq 'vertical'){$base}else{$base+1};$reference=$images[$template].Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $slots=if($orientation -eq 'vertical'){@($base,($base+2))}else{@(($base+1),($base+3))}
 foreach($slot in $slots){if($orientation -eq 'vertical'){Fit-FeatureEdge $slot 'N' $reference 'N' 'F162 vertical standard';Fit-FeatureEdge $slot 'S' $reference 'N' 'F162 vertical standard'}else{Fit-FeatureEdge $slot 'W' $reference 'W' 'F162 horizontal standard';Fit-FeatureEdge $slot 'E' $reference 'W' 'F162 horizontal standard'}}
 $reference.Dispose()
}

# Restore unwarped green texture in target-space grass well away from bank/foam and connector collars.
foreach($slot in @($matchedBands|ForEach-Object slot|Sort-Object -Unique)){
 $im=$images[$slot];$snapshot=$im.Clone([System.Drawing.Rectangle]::new(0,0,64,64),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb);$baseline=$featureOriginals[$slot]
 $protectedEdges=@($matchedBands|Where-Object {$_.slot -eq $slot}|ForEach-Object edge)
 for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
  $protected=($x -lt 4 -and 'W' -in $protectedEdges) -or ($x -ge 60 -and 'E' -in $protectedEdges) -or ($y -lt 4 -and 'N' -in $protectedEdges) -or ($y -ge 60 -and 'S' -in $protectedEdges)
  if($protected){continue}
  $deep=$true
  foreach($delta in @(@(0,0),@(-5,0),@(5,0),@(0,-5),@(0,5))){
   $xx=[Math]::Max(0,[Math]::Min(63,$x+$delta[0]));$yy=[Math]::Max(0,[Math]::Min(63,$y+$delta[1]));$c=$snapshot.GetPixel($xx,$yy)
   if(-not($c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.2)){$deep=$false;break}
  }
  if($deep){$c=$baseline.GetPixel($x,$y);if(-not($c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.2)){$c=$grassFill.GetPixel($x,$y)};$im.SetPixel($x,$y,$c)}
 }}
 $snapshot.Dispose()
}
# Horizontal channels have open grass along the outer north/south image edges.
# Remove generator separator residues there without moving any bank or water pixels.
foreach($slot in @(((162-120)*4+1),((162-120)*4+3))){
 $im=$images[$slot]
 for($x=0;$x -lt 64;$x++){for($d=0;$d -lt 9;$d++){
  foreach($y in @($d,(63-$d))){
   $c=$im.GetPixel($x,$y)
   if($c.G -ge $c.R*.92 -and $c.G -gt $c.B*1.5){$im.SetPixel($x,$y,$grassFill.GetPixel($x,$y))}
  }
 }}
}
# Refresh only clear water AFTER endpoint remapping; leave connector bands and shoreline foam intact.
foreach($slot in 136..191){
 $im=$images[$slot];$distance=[int[,]]::new(64,64)
 for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){$c=$im.GetPixel($x,$y);$distance[$x,$y]=if($c.B -gt $c.R*1.5 -and $c.B -gt $c.G*1.05){100}else{0}}}
 for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){if($x -gt 0){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[($x-1),$y]+1))};if($y -gt 0){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[$x,($y-1)]+1))}}}
 for($y=63;$y -ge 0;$y--){for($x=63;$x -ge 0;$x--){if($x -lt 63){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[($x+1),$y]+1))};if($y -lt 63){$distance[$x,$y]=[Math]::Min(($distance[$x,$y]),($distance[$x,($y+1)]+1))}}}
 $protectedEdges=@($matchedBands|Where-Object {$_.slot -eq $slot}|ForEach-Object edge)
 for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
  $protected=($x -lt 4 -and 'W' -in $protectedEdges) -or ($x -ge 60 -and 'E' -in $protectedEdges) -or ($y -lt 4 -and 'N' -in $protectedEdges) -or ($y -ge 60 -and 'S' -in $protectedEdges)
  if(-not $protected -and $distance[$x,$y] -ge 4){$im.SetPixel($x,$y,$water.GetPixel($x,$y))}
 }}
}

Write-Output ('Matched feature endpoint bands:'+ $matchedBands.Count+' skipped:'+ $unmatched.Count)

$atlas=[System.Drawing.Bitmap]::new(1024,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[System.Drawing.Graphics]::FromImage($atlas)
foreach($slot in 0..191){$g.DrawImageUnscaled($images[$slot],($slot%16)*64,([int][Math]::Floor($slot/16))*64)}
$g.Dispose();$atlas.Save((Join-Path $dir 'water-grass-all-directions-64.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$map=[pscustomobject]@{image='water-grass-all-directions-64.png';width=1024;height=768;cellSize=64;columns=16;rows=12;padding=0;margin=0;slotCount=192;familyCount=48;orientations=@(0,90,180,270);orientationMeaning='Clockwise SHAPE orientation, world artwork remains upright. Four adjacent cells per family; four families per row.';coreEquivalentSlots=136;newSpecialSlots=42;originalSpecialSlots=14;matchedFeatureBands=$matchedBands;unmatchedFeatureBands=$unmatched;tiles=$records;limitations=@('192 slots are not192 unique shapes: directional-equivalent core images intentionally repeat.','Core variants are equivalent shape classes, not literal pixel-rotated silhouettes. Special new art follows guide topology, not exact source pixel masks.','New special endpoints require suitable mates; arbitrary all-to-all joins are not guaranteed.','No Unity import, metadata or scene changes.')}
$map|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $dir 'water-grass-all-directions-64.json') -Encoding UTF8
$font=[System.Drawing.Font]::new('Consolas',11)
$guide=[System.Drawing.Bitmap]::new(2048,1848);$g=[System.Drawing.Graphics]::FromImage($guide);$g.Clear([System.Drawing.Color]::FromArgb(18,25,35));$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
foreach($slot in 0..191){$x=($slot%16)*128;$y=[int][Math]::Floor($slot/16)*154;$rec=$records[$slot];$g.DrawString(('F'+$rec.familySourceIndex+' / '+$rec.shapeOrientationClockwise),$font,[System.Drawing.Brushes]::White,$x,$y);$g.DrawImage($images[$slot],[System.Drawing.Rectangle]::new($x,$y+22,128,128))}
$g.Dispose();$guide.Save((Join-Path $dir 'water-grass-directions-guide.png'));$guide.Dispose()
foreach($part in 0..1){
 $proof=[System.Drawing.Bitmap]::new(768,1120);$g=[System.Drawing.Graphics]::FromImage($proof);$g.Clear([System.Drawing.Color]::FromArgb(18,25,35));$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
 for($r=0;$r -lt 7;$r++){$family=154+$part*7+$r;for($q=0;$q -lt 4;$q++){$slot=($family-120)*4+$q;$g.DrawString(('F'+$family+' / '+($q*90)),$font,[System.Drawing.Brushes]::White,$q*192,$r*160);$g.DrawImage($images[$slot],[System.Drawing.Rectangle]::new($q*192,$r*160+24,128,128))}}
 $g.Dispose();$proof.Save((Join-Path $dir ('specials-proof-'+($part+1)+'.png')));$proof.Dispose()
}

# A mixed actual-tile assembly: existing core banks/corners, new oriented bays and matched land tongues.
$mix=[System.Drawing.Bitmap]::new(1280,1096);$mg=[System.Drawing.Graphics]::FromImage($mix);$mg.Clear([System.Drawing.Color]::FromArgb(18,25,35));$mg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$mg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$mg.DrawString('CORE + ORIENTED BAYS + PAIRED TONGUES (NO SPRITE ROTATION)',$font,[System.Drawing.Brushes]::White,0,0)
for($r=0;$r -lt 7;$r++){for($c=0;$c -lt 8;$c++){
 $slot=if($r -eq 0 -and $c -eq 0){(150-120)*4}elseif($r -eq 0 -and $c -eq 7){(151-120)*4}elseif($r -eq 6 -and $c -eq 0){(152-120)*4}elseif($r -eq 6 -and $c -eq 7){(153-120)*4}elseif($r -eq 0){0}elseif($r -eq 6){(126-120)*4}elseif($c -eq 0){(132-120)*4}elseif($c -eq 7){(138-120)*4}else{(148-120)*4}
 if($r -eq 0 -and $c -eq 3){$slot=(157-120)*4}
 if($r -eq 6 -and $c -eq 3){$slot=(157-120)*4+2}
 if($c -eq 0 -and $r -eq 3){$slot=(157-120)*4+3}
 if($c -eq 7 -and $r -eq 3){$slot=(157-120)*4+1}
 if($r -eq 2 -and $c -eq 2){$slot=(163-120)*4}
 if($r -eq 3 -and $c -eq 2){$slot=(163-120)*4+2}
 if($r -eq 3 -and $c -eq 4){$slot=(163-120)*4+3}
 if($r -eq 3 -and $c -eq 5){$slot=(163-120)*4+1}
 $mg.DrawImage($images[$slot],[System.Drawing.Rectangle]::new($c*128,24+$r*128,128,128))
}}
$mg.DrawString('MATCHED CHANNELS: HORIZONTAL / VERTICAL',$font,[System.Drawing.Brushes]::White,0,932)
foreach($q in 0..1){$slot=(162-120)*4+1+$q*2;$mg.DrawImage($images[$slot],[System.Drawing.Rectangle]::new($q*128,956,128,128))}
foreach($q in 0..1){$slot=(162-120)*4+$q*2;$mg.DrawImage($images[$slot],[System.Drawing.Rectangle]::new(1050,24+$q*128,128,128))}
$mg.Dispose();$mix.Save((Join-Path $dir 'mixed-assembly-proof.png'));$mix.Dispose()

$coreCompared=0;$coreDifferent=0
foreach($record in $records){if($record.familySourceIndex -ge 154){continue};$sheet=if($record.artMode -eq 'v03_world_oriented_corner'){$repaired}else{$old};$index=[int]$record.copiedSourceIndex;$same=$true;for($y=0;$y -lt 64 -and $same;$y++){for($x=0;$x -lt 64;$x++){if($images[$record.index].GetPixel($x,$y).ToArgb() -ne $sheet.GetPixel(($index%16)*64+$x,([int][Math]::Floor($index/16))*64+$y).ToArgb()){$same=$false;break}}};$coreCompared++;if(-not $same){$coreDifferent++}}
Write-Output "FINAL:192 cells;136 direct core copies compared:$coreCompared differences:$coreDifferent; rear cleanup:$rearCount fixed water:$waterCount"
$font.Dispose();$atlas.Dispose();$old.Dispose();$repaired.Dispose();$water.Dispose();$grassFill.Dispose();foreach($b in $featureOriginals.Values){$b.Dispose()};foreach($b in $images.Values){$b.Dispose()}
