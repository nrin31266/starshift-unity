local root=assert(app.params.root);local out=assert(app.params.out)
local function channel(v,k)return k=='r' and app.pixelColor.rgbaR(v) or k=='g' and app.pixelColor.rgbaG(v) or app.pixelColor.rgbaB(v)end
local function animate(source,id,kind,count)
 local base=Image{fromFile=root..'/raw/'..source..'.png'}
 local sprite=Sprite(64,64,ColorMode.RGB);local sheet=Image(256,64*math.ceil(count/4),ColorMode.RGB)
 for f=0,count-1 do
  local im=Image(64,64,ColorMode.RGB);local t=f/(count-1)
  local scale=kind=='hit' and (.35+math.min(t/.4,1)*.75) or kind=='focus' and (1.1-t*.28) or (.72+t*.40)
  for y=0,63 do for x=0,63 do
   local sx,sy=math.floor((x-31.5)/scale+32),math.floor((y-31.5)/scale+32)
   if sx>=0 and sx<64 and sy>=0 and sy<64 then
    local v=base:getPixel(sx,sy);local a=app.pixelColor.rgbaA(v)
    if kind=='hit' and t>.45 then
     local radius=math.sqrt((x-31.5)^2+(y-31.5)^2)
     if radius<(t-.45)*46 then a=0 else a=math.floor(a*(1-t)*1.8)end
    end
    local light=kind=='hit' and math.max(0,1-t*3)*40 or kind=='focus' and t*24 or t*35
    if a>0 then im:putPixel(x,y,app.pixelColor.rgba(math.min(255,channel(v,'r')+light),math.min(255,channel(v,'g')+light),math.min(255,channel(v,'b')+light),a))end
   end
  end end
  if f>0 then sprite:newFrame()end
  sprite:newCel(sprite.layers[1],f+1,im,Point(0,0));sprite.frames[f+1].duration=kind=='hit' and .035 or .08
  sheet:drawImage(im,Point(f%4*64,math.floor(f/4)*64))
  if f==0 then im:saveAs(out..'/'..id..'.png')end
 end
 sprite:saveAs(root..'/'..id..'.aseprite');sprite:close();sheet:saveAs(out..'/'..id..'-sheet.png')
end
for _,family in ipairs{'pulse','plasma','homing','rock','shield'}do animate('impact-'..family,'hit-'..family,'hit',8)end
for _,family in ipairs{'gun','sniper','strike','missile'}do animate('focus-'..family,'focus-'..family,'focus',4)end
for _,family in ipairs{'vanguard-curtain','vanguard-lane-beam','warden-ion-shelter','warden-beam-sweep'}do animate(family,family..'-cue','charge',8)end
animate('boss-armor-rupture','boss-armor-rupture','hit',8)
animate('supply-burst','supply-burst-polished','hit',8)
animate('thermal-vent','thermal-vent','hit',8)
-- Animation v3 đã review: frame đầu là anchor tĩnh, dùng tám frame chuyển động sau đó.
local raw=Image{fromFile=root..'/raw/curtain-animation.png'}
local s=Sprite(64,64,ColorMode.RGB);local strip=Image(256,128,ColorMode.RGB)
for i=0,7 do
 local im=Image(64,64,ColorMode.RGB);im:drawImage(raw,Point(-(i+1)*64,-64))
 if i>0 then s:newFrame()end;s:newCel(s.layers[1],i+1,im,Point(0,0));s.frames[i+1].duration=.055
 strip:drawImage(im,Point(i%4*64,math.floor(i/4)*64))
end
s:saveAs(root..'/vanguard-curtain-pulse.aseprite');s:close();strip:saveAs(out..'/vanguard-curtain-pulse-sheet.png')
local armor=Image{fromFile=root..'/raw/boss-armor-rupture.png'}
for i,seed in ipairs{{26,5,7,5},{38,7,38,7}}do
 local im=Image(32,32,ColorMode.RGB);local seen={};local todo={{seed[1],seed[2]}}
 while #todo>0 do
  local p=table.remove(todo);local x,y=p[1],p[2];local key=y*64+x
  if x>=0 and x<64 and y>=0 and y<64 and not seen[key] then
   seen[key]=true;local v=armor:getPixel(x,y)
   if app.pixelColor.rgbaA(v)>0 then
    local xx,yy=x-seed[3]+1,y-seed[4]+2
    if xx>=0 and xx<32 and yy>=0 and yy<32 then im:putPixel(xx,yy,v)end
    todo[#todo+1]={x-1,y};todo[#todo+1]={x+1,y};todo[#todo+1]={x,y-1};todo[#todo+1]={x,y+1}
   end
  end
 end
 local sprite=Sprite(32,32,ColorMode.RGB);sprite:newCel(sprite.layers[1],1,im,Point(0,0));sprite:saveAs(root..'/boss-armor-fragment-'..(i-1)..'.aseprite');sprite:close();im:saveAs(out..'/boss-armor-fragment-'..(i-1)..'.png')
end
for _,spec in ipairs{{'shelter-animation','warden-shelter-pulse'},{'sweep-animation','warden-sweep-pulse'}}do
 local atlas=Image{fromFile=root..'/raw/'..spec[1]..'.png'}
 local pulse=Sprite(64,64,ColorMode.RGB);local pulseSheet=Image(256,256,ColorMode.RGB)
 for i=0,15 do
  local im=Image(64,64,ColorMode.RGB);im:drawImage(atlas,Point(-i*64,-64))
  if i>0 then pulse:newFrame()end;pulse:newCel(pulse.layers[1],i+1,im,Point(0,0));pulse.frames[i+1].duration=.035
  pulseSheet:drawImage(im,Point(i%4*64,math.floor(i/4)*64))
 end
 pulse:saveAs(root..'/'..spec[2]..'.aseprite');pulse:close();pulseSheet:saveAs(out..'/'..spec[2]..'-sheet.png')
end
-- Ribbon lặp theo trục beam: cạnh còn đọc được đúng bề rộng damage, lõi có vân sáng.
local beam=Image(128,32,ColorMode.RGB)
for y=0,31 do for x=0,127 do
 local side=math.abs(y-15.5)/15.5
 local ripple=math.sin(x/128*math.pi*8+y*.8)
 local bright=side<.25 and 255 or math.floor(180+(1-side)*60+ripple*12)
 local alpha=side>.94 and 100 or side>.8 and 180 or 255
 if (x+math.floor(y/4)*9)%32<2 and side>.3 then bright=255 end
 beam:putPixel(x,y,app.pixelColor.rgba(bright,bright,255,alpha))
end end
local ribbon=Sprite(128,32,ColorMode.RGB);ribbon:newCel(ribbon.layers[1],1,beam,Point(0,0));ribbon:saveAs(root..'/beam-energy.aseprite');ribbon:close();beam:saveAs(out..'/beam-energy.png')
local field=Image(128,64,ColorMode.RGB)
for y=0,63 do for x=0,127 do
 local strand=(y+math.floor(math.sin(x/128*math.pi*4+y*.3)*2))%10
 local alpha=strand<1 and 210 or strand<3 and 100 or 24
 if strand<3 and x%24<3 then alpha=255 end
 field:putPixel(x,y,app.pixelColor.rgba(210,235,255,alpha))
end end
local fieldSprite=Sprite(128,64,ColorMode.RGB);fieldSprite:newCel(fieldSprite.layers[1],1,field,Point(0,0));fieldSprite:saveAs(root..'/ion-curtain.aseprite');fieldSprite:close();field:saveAs(out..'/ion-curtain.png')
local bolt=Image(96,16,ColorMode.RGB)
for y=0,15 do for x=0,95 do
 local dy=math.abs(y-7.5);local cap=math.max(0,5-x,5-(95-x))
 if dy<8-cap then
  local r,g,b=255,dy<2 and 250 or dy<4 and 203 or 126,dy<2 and 204 or dy<4 and 83 or 39
  if x%24<3 and dy<4 then r,g,b=255,255,234 end
  bolt:putPixel(x,y,app.pixelColor.rgba(r,g,b,dy>6 and 150 or 255))
 end
end end
local projectile=Sprite(96,16,ColorMode.RGB);projectile:newCel(projectile.layers[1],1,bolt,Point(0,0));projectile:saveAs(root..'/vanguard-curtain-bolt.aseprite');projectile:close();bolt:saveAs(out..'/vanguard-curtain-bolt.png')
