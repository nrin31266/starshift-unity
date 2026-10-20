local root=assert(app.params.root);local out=assert(app.params.out)
local function color(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local ramps={
 scout={'122b3f','264c65','41758e','77aec2','c8e3e8'},
 raider={'142338','294461','4a6f90','88abc3','d8e5ec'},
 heavy={'30231b','64432a','a16a38','dba65d','f6ddb0'},
 bulwark={'282c1f','4a5234','7d8954','b2be82','eeeac6'},
 sniper={'211c39','403666','695b9d','a194cf','e1d8f0'},
 interceptor={'1a2429','36434a','566971','91a2a5','d9dfd5'},
 carrier={'173138','305965','538c94','94c8c9','d7ebe3'},
 vanguard={'251f32','493a55','796682','ac94b5','e1cfdb'},
 warden={'201e3a','3b3b63','676a9a','aaaed5','dce2f5'}
}
local function save(im,id)
 local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root..'/'..id..'.aseprite');s:saveAs(out..'/'..id..'.png');s:close()
end
local function opaque(im,x,y)return x>=0 and y>=0 and x<im.width and y<im.height and app.pixelColor.rgbaA(im:getPixel(x,y))>0 end
local function count(im)local n=0;for px in im:pixels()do if app.pixelColor.rgbaA(px())>0 then n=n+1 end end;return n end
local function repaint(raw,key)
 local im=Image(raw);local ramp=ramps[key]
 for px in im:pixels()do
  local v=px();local a=app.pixelColor.rgbaA(v)
  if a<128 then px(0)else
   local r,g,b=app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v)
   local value=.25*r+.55*g+.2*b
   -- Accent gốc giữ vai trò sensor/weapon; armor map theo value, không lookup palette rơi về đen.
   local accent=value>70 and (math.max(r,g,b)-math.min(r,g,b)>55) and (r>g*1.5 or (b>r*1.5 and b>g*1.3))
   if key=='interceptor' then accent=accent and value>185 end
   if accent then
    local accents=key=='scout' and {'216f8c','55cde2','d5f9f5'} or key=='raider' and {'983f50','ed7f7a','ffe0ca'} or key=='interceptor' and {'638132','bddb60','f0ffd0'} or key=='carrier' and {'357b91','71cad5','d4f6fa'} or key=='sniper' and {'6552a1','b3a0ef','eee7ff'} or key=='vanguard' and {'7e2d43','d75f70','ffb6a3'} or key=='warden' and {'247c84','6dded3','d5fff1'} or {'a56c34','e4b36a','fff0bd'}
    px(color(accents[value<90 and 1 or value<180 and 2 or 3]))
   else px(color(ramp[value<40 and 1 or value<85 and 2 or value<135 and 3 or value<195 and 4 or 5]))end
  end
 end
 return im
end
local function chip(im,side,ratio,radius)
 local cy=math.floor(im.height*ratio)
 for y=math.max(0,cy-radius),math.min(im.height-1,cy+radius)do
  local edge=nil
  for step=0,im.width-1 do local x=side<0 and step or im.width-1-step;if opaque(im,x,y)then edge=x;break end end
  if edge then
   local depth=math.floor(radius*(1-math.abs(y-cy)/(radius+1))*(1.0+((y%4==0)and .2 or 0)))
   for step=0,depth do local x=edge-side*step;if opaque(im,x,y)then im:putPixel(x,y,0)end end
   -- Mép đứt có thành giáp sáng và khe máy tối, chỉ vẽ lên pixel hull còn lại.
   local inner=edge-side*(depth+1)
   if opaque(im,inner,y)then im:putPixel(inner,y,color('c3cbd1'))end
   if opaque(im,inner-side,y)then im:putPixel(inner-side,y,color('273849'))end
  end
 end
end
local function damage(im,id)
 local intact=count(im);local damaged=Image(im)
 chip(damaged,-1,.42,math.max(4,math.floor(im.width*.12)))
 local critical=Image(damaged);chip(critical,1,.63,math.max(6,math.floor(im.width*.19)))
 assert(count(damaged)<intact,id..': no damaged silhouette loss')
 assert(count(critical)<count(damaged),id..': no critical silhouette loss')
 save(im,id);save(damaged,id..'-damaged');save(critical,id..'-critical')
end
for _,id in ipairs{'scout','raider','heavy','bulwark','sniper','interceptor','carrier','vanguard-hull','warden-hull'}do
 local p=root..'/raw/'..id..'.png';if not app.fs.isFile(p)then p=root..'/../sources/'..id..'-raw.png'end
 local raw=Image{fromFile=p};local key=id:gsub('%-hull$','')
 if id=='sniper' then
  -- Wing trái là mẫu có chủ đích; mirror silhouette, giữ shading khác nhau ở pass repaint.
  for y=0,raw.height-1 do for x=0,math.floor(raw.width/2)-1 do raw:putPixel(raw.width-1-x,y,raw:getPixel(x,y))end end
 end
 if id=='bulwark' then
  -- Bỏ flame baked vào raw cũ; nozzle vẫn giữ trong hull.
  for y=raw.height-13,raw.height-1 do for x=math.floor(raw.width*.4),math.floor(raw.width*.6)do raw:putPixel(x,y,0)end end
 end
 local im=repaint(raw,key);damage(im,id)
 if id:find('%-hull$') then
  local short=id:gsub('%-hull$','');local damaged=Image{fromFile=out..'/'..id..'-damaged.png'};local critical=Image{fromFile=out..'/'..id..'-critical.png'}
  save(damaged,short..'-damaged');save(critical,short..'-critical')
 end
end
local drone=Image{fromFile=out..'/scout.png'};drone:resize(48,38)
local canvas=Image(48,48,ColorMode.RGB);canvas:drawImage(drone,Point(0,5));damage(canvas,'warden-drone')
local rock=Image{fromFile=root..'/../sources/asteroid-anchor-raw.png'}
for _,spec in ipairs{{'small',48},{'medium',64},{'large',96},{'huge',128}}do
 local im=Image(rock);im:resize(spec[2],spec[2]);damage(im,'asteroid-'..spec[1])
end
local resource=Image(rock);resource:resize(96,96)
for _,spec in ipairs{{42,37,8,18},{55,45,6,14},{34,51,5,11}}do
 local cx,cy,w,h=table.unpack(spec)
 for y=cy-h,cy+h do local span=math.floor(w*(1-math.abs(y-cy)/(h+1)))
  for x=cx-span,cx+span do resource:putPixel(x,y,color(x<cx and 'b88135' or x==cx and 'fff0b4' or 'f4c868'))end
 end
end
damage(resource,'resource-asteroid')
print('PASS: fleet/asteroid damaged and critical alpha counts strictly decrease')
