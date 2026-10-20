local root=assert(app.params.root)
local out=assert(app.params.out)
local function rgba(h) return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
local function save(im,id)
 local s=Sprite(im.width,im.height,ColorMode.RGB)
 s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root.."/"..id..".aseprite");s:saveAs(out.."/"..id..".png");s:close()
end
-- Chỉ repaint hai nozzle bị vẽ chồng; hull/skin A ngoài vùng này giữ nguyên.
for _,id in ipairs{'player','player-amber','player-swept','player-swept-amber'} do
 local im=Image{fromFile=root..'/approved/'..id..'.png'}
 local swept=id:find('swept')~=nil;local bottom=swept and 72 or 68
 local centers=swept and {23,41} or {22,41}
 for _,cx in ipairs(centers)do
  for y=bottom-8,79 do for x=cx-6,cx+6 do im:putPixel(x,y,0)end end
  for y=bottom-10,bottom-1 do
   local half=(y<bottom-2)and 4 or 3
   for x=cx-half,cx+half do
    local h=x==cx-half and 'a6bfcb' or x==cx+half and '1c2e46' or x<cx and '6e899b' or '46617a'
    if y>=bottom-4 then h=(x>cx-half and x<cx+half)and '0b2639' or '6e899b'end
    if y==bottom-3 and x>=cx-1 and x<=cx+1 then h='41b9c8'end
    im:putPixel(x,y,rgba(h))
   end
  end
 end
 save(im,id)
end
local function sheet(id,w,h,frames,seconds)
 local s=Sprite(w,h,ColorMode.RGB);local strip=Image(w*#frames,h,ColorMode.RGB)
 for i,im in ipairs(frames) do if i>1 then s:newFrame() end;s:newCel(s.layers[1],i,im,Point(0,0));s.frames[i].duration=seconds;strip:drawImage(im,Point((i-1)*w,0)) end
 s:saveAs(root.."/"..id..".aseprite");s:close();frames[1]:saveAs(out.."/"..id..".png");strip:saveAs(out.."/"..id.."-sheet.png")
end
local frames={}
for f=0,3 do
 local im=Image(32,40,ColorMode.RGB)
 for y=2,32 do
  local edge=math.max(0,math.floor(5*(1-(y-2)/(30+(f%2)*2))))
  for x=16-edge,15+edge do
   local d=math.abs(x-15.5)/math.max(1,edge)
   im:putPixel(x,y,rgba(d<.3 and y<17 and 'efffff' or d<.65 and '78e8ef' or '258eae'))
  end
 end
 frames[#frames+1]=im
end
sheet('engine-loop',32,40,frames,.075)
for _,id in ipairs{'trail-standard','trail-ion','trail-rainbow'} do
 local frames={}
 for f=0,3 do
  local im=Image(16,64,ColorMode.RGB)
  for y=0,63 do
   local width=math.max(1,math.floor(4*(1-y/64)))
   for x=8-width,7+width do local colors=id=='trail-rainbow' and {'f36a73','f6c46a','edec8e','66d8d4','87a2ef','c194e9'} or id=='trail-ion' and {'e5ffff','9beff4','64d3e9','47a9ce','277596','164b68'} or {'a0efec','65d8da','3ba9bc','2d829b','22546e','17384f'}
   local h=colors[math.min(6,1+math.floor((id=='trail-rainbow' and (x-8+width)/(width*2) or y/64)*6))]
   local v=rgba(h);im:putPixel(x,y,app.pixelColor.rgba(app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v),math.floor((200+f*8)*(1-y/64)))) end
  end
  frames[#frames+1]=im
 end
 sheet(id,16,64,frames,.1)
end

local hull=Image{fromFile=root..'/approved/player.png'}
for i,spec in ipairs{{7,34},{35,34},{16,52},{34,52}}do
 local fragment=Image(24,28,ColorMode.RGB);fragment:drawImage(hull,Point(-spec[1],-spec[2]));save(fragment,'player-fragment-'..(i-1))
end
