local root=assert(app.params.root);local out=assert(app.params.out)
local rgba=app.pixelColor.rgba
local function blank(w,h)return Image(w,h,ColorMode.RGB)end
local function alpha(v)return app.pixelColor.rgbaA(v)end
local function save(id,frames,seconds)
 local w,h=frames[1].width,frames[1].height;local s=Sprite(w,h,ColorMode.RGB);local sheet=blank(w*4,h*math.ceil(#frames/4))
 for i,im in ipairs(frames)do
  if i>1 then s:newFrame()end;s:newCel(s.layers[1],i,im,Point(0,0));s.frames[i].duration=seconds or .04
  sheet:drawImage(im,Point((i-1)%4*w,math.floor((i-1)/4)*h))
 end
 s:saveAs(root..'/'..id..'.aseprite');s:close();frames[1]:saveAs(out..'/'..id..'.png')
 if #frames>1 then sheet:saveAs(out..'/'..id..'-sheet.png')end
end
local function crop(im)
 local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if alpha(im:getPixel(x,y))>8 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
 assert(x1>=x0,'Empty source');local result=blank(x1-x0+1,y1-y0+1);result:drawImage(im,Point(-x0,-y0));return result
end
local function drawn(source,id,kind,count)
 local base=Image{fromFile=root..'/raw/'..source..'.png'};local frames={}
 for i=0,count-1 do
  local im=blank(64,64);local t=i/(count-1);local scale=kind=='focus' and (1-t*.15) or (.4+math.sin(math.min(1,t*1.5)*math.pi*.5)*.85)
  for y=0,63 do for x=0,63 do
   local sx,sy=math.floor((x-31.5)/scale+32),math.floor((y-31.5)/scale+32)
   if sx>=0 and sx<64 and sy>=0 and sy<64 then
    local v=base:getPixel(sx,sy);local a=alpha(v)
    if kind~='focus' then
     if t>.45 and ((x-31.5)^2+(y-31.5)^2)^.5<(t-.45)*46 then a=0 end
     a=math.floor(a*math.min(1,(1-t)*2))
    end
    local glint=kind=='focus' and math.floor(20*math.sin(t*math.pi)) or math.max(0,math.floor((.3-t)*90))
    if a>0 then im:putPixel(x,y,rgba(math.min(255,app.pixelColor.rgbaR(v)+glint),math.min(255,app.pixelColor.rgbaG(v)+glint),math.min(255,app.pixelColor.rgbaB(v)+glint),a))end
   end
  end end
  frames[#frames+1]=im
 end
 save(id,frames,.04)
end
for _,family in ipairs{'pulse','plasma','homing','rock'}do drawn('hit-'..family,'hit-'..family,'hit',8)end
drawn('strike-launch','strike-launch','hit',8)
drawn('heavy-death','heavy-burst','hit',8)
drawn('missile-blast','missile-splash','hit',8)
drawn('shield-glint','pickup-shield','hit',8)
drawn('coin-collect','pickup-coin','hit',8)
drawn('upgrade','pickup-collect-upgrade','hit',8)
drawn('supply-burst','supply-burst-polished','hit',8)
-- Bỏ frame 0 nhiễu của animation Pro; chọn keyframe xuyên suốt, giữ kết thúc trong suốt.
for _,spec in ipairs{{'enemy-death-animation','explosion',8},{'boss-death-animation','boss-blast',16}}do
 local raw=Image{fromFile=root..'/raw/'..spec[1]..'.png'};local frames={}
 for i=0,spec[3]-1 do
  local index=spec[3]==8 and (1+i*2) or math.min(15,i+1)
  local im=blank(64,64);im:drawImage(raw,Point(-index*64,-64));frames[#frames+1]=im
 end
 frames[#frames]=blank(64,64);save(spec[2],frames,spec[3]==8 and .0625 or .04)
end
local lock=Image{fromFile=root..'/raw/strike-lock-animation.png'};local locks={}
for i=0,7 do local im=blank(64,64);im:drawImage(lock,Point(-i*64,-64));locks[#locks+1]=im end
save('focus-strike',locks,.07)
local arrow=Image{fromFile=root..'/raw/sweep-arrow.png'};save('warden-sweep-arrow',{arrow})
local base=crop(Image{fromFile=root..'/raw/curtain-bolt.png'});base:resize(92,20);local bolts={}
for i=0,3 do
 local im=blank(96,24);im:drawImage(base,Point(2,2))
 for x=9+i*17,math.min(94,15+i*17)do for y=7,15 do
  local v=im:getPixel(x,y);if alpha(v)>0 then im:putPixel(x,y,rgba(255,255,220,alpha(v)))end
 end end
 bolts[#bolts+1]=im
end
save('vanguard-curtain-bolt',bolts,.06)
for _,id in ipairs{'shield','overdrive','support'}do
 local image=crop(Image{fromFile=root..'/raw/ui-'..id..'.png'});local ratio=38/math.max(image.width,image.height)
 image:resize(math.max(1,math.floor(image.width*ratio+.5)),math.max(1,math.floor(image.height*ratio+.5)))
 local icon=blank(48,48);icon:drawImage(image,Point(math.floor((48-image.width)/2),math.floor((48-image.height)/2)));save('ui-'..id,{icon})
end
