local root=assert(app.params.root);local out=assert(app.params.out)
local function c(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function rect(im,x,y,w,h,hx)for yy=math.floor(y),math.floor(y+h-1)do for xx=math.floor(x),math.floor(x+w-1)do if xx>=0 and yy>=0 and xx<im.width and yy<im.height then im:putPixel(xx,yy,c(hx))end end end end
local function poly(im,p,hx)
 for y=0,im.height-1 do local xs={};for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end;table.sort(xs);for i=1,#xs-1,2 do rect(im,math.ceil(xs[i]),y,math.floor(xs[i+1])-math.ceil(xs[i])+1,1,hx)end end
end
local function disc(im,cx,cy,r,hx)
 for y=math.floor(cy-r),math.ceil(cy+r)do for x=math.floor(cx-r),math.ceil(cx+r)do if (x-cx)^2+(y-cy)^2<=r*r then rect(im,x,y,1,1,hx)end end end
end
local function save(id,frames,seconds)
 local w,h=frames[1].width,frames[1].height;local s=Sprite(w,h,ColorMode.RGB);local sheet=image(w*4,h*math.ceil(#frames/4))
 for i,im in ipairs(frames)do if i>1 then s:newFrame()end;s:newCel(s.layers[1],i,im,Point(0,0));s.frames[i].duration=seconds or .06;sheet:drawImage(im,Point((i-1)%4*w,math.floor((i-1)/4)*h))end
 s:saveAs(root..'/'..id..'.aseprite');s:close();frames[1]:saveAs(out..'/'..id..'.png');sheet:saveAs(out..'/'..id..'-sheet.png')
end
for _,spec in ipairs{{'hostile-coral','b84c55','ff9b81'},{'hostile-lemon','aa873d','fff2a2'},{'hostile-special','914394','e69fe4'},{'pulse-bolt','287c9c','8ce9f2'}}do
 local frames={}
 for f=0,3 do local a=image(16,24);local wide=(f%2==0)and 4 or 3
  poly(a,{{8,2},{8+wide,7},{8+wide,15},{10,20},{6,20},{8-wide,15},{8-wide,7}},spec[2]);rect(a,6,6,4,11,spec[3]);rect(a,7,5,2,8+f,'f5fff5');frames[#frames+1]=a
 end
 save(spec[1],frames,.08)
end
local missiles={};local plasma={}
for f=0,3 do
 local a=image(20,36)
 poly(a,{{10,1},{14,8},{14,24},{18,29},{18,32},{12,29},{8,29},{2,32},{2,29},{6,24},{6,8}},'244154')
 poly(a,{{10,2},{13,9},{13,25},{10,29},{7,25},{7,9}},'c9dedd')
 poly(a,{{10,2},{13,9},{7,9}},'ffc978');rect(a,8,12,2,12,'f4fff5');rect(a,11,12,2,12,'6e97a8')
 poly(a,{{8,29},{12,29},{10,34-f%2*2}},'79e6ed');missiles[#missiles+1]=a
 local orb=image(28,28)
 disc(orb,14,14,10,'4c477f');disc(orb,13,13,8,'887bc9');disc(orb,11,10,5,'c6c4f3');disc(orb,10,9,2,'f7ffff')
 rect(orb,21,12+f,2,3,'b6adf1');plasma[#plasma+1]=orb
end
save('player-homing',missiles,.08);save('reserve-missile',missiles,.08)
save('plasma-orb',plasma,.08)
for _,kind in ipairs{'pulse','plasma','missile','beam'}do
 local muzzle={};local hits={}
 local accent=kind=='pulse' and '69dcec' or kind=='plasma' and 'ab9ee7' or kind=='missile' and 'e8aa63' or 'de9be1'
 local edge=kind=='pulse' and '267f9c' or kind=='plasma' and '655b9e' or kind=='missile' and 'a56b3d' or '9a569e'
 for f=0,3 do
  local a=image(32,32);local length=({21,28,17,8})[f+1];local spread=({5,8,6,3})[f+1]
  poly(a,{{16,31-length},{16+spread,25},{19,30},{13,30},{16-spread,25}},edge)
  poly(a,{{16,33-length},{16+spread-2,25},{17,29},{15,29},{18-spread,25}},accent)
  rect(a,15,math.max(6,31-length+5),2,math.min(14,length-2),'f4fff5')
  if kind=='plasma' then disc(a,16,23,4+f%2,accent);disc(a,16,23,2,'fff4f6')end
  muzzle[#muzzle+1]=a
  local hit=image(64,64)
  if kind=='plasma' then
   local r=({5,13,17,9})[f+1]
   for i=0,4 do local t=i*math.pi*.4+f*.35;disc(hit,32+math.cos(t)*r*.65,32+math.sin(t)*r*.65,r*.65,edge)end
   disc(hit,31,30,r*.75,accent);disc(hit,29,28,r*.35,'f1f4ff')
  elseif kind=='missile' then
   local r=({6,15,20,11})[f+1]
   for i=0,4 do local t=i*math.pi*.4;disc(hit,32+math.cos(t)*r*.5,32+math.sin(t)*r*.5,r*.6,f>1 and '705a51' or edge)end
   disc(hit,31,29,r*.65,accent);disc(hit,29,27,r*.35,'fff0c4')
  else
   local r=({7,15,19,9})[f+1]
   poly(hit,{{32,32-r},{36,28},{32+r,32},{37,35},{32,32+r},{29,36},{32-r,32},{27,29}},accent)
   poly(hit,{{32,26},{37,32},{32,38},{27,32}},'f4fff6')
  end
  for i=0,5 do local t=i*math.pi/3+f*.2;local d=8+f*6;local x=32+math.cos(t)*d;local y=32+math.sin(t)*d
   poly(hit,{{x-2,y-2},{x+3,y-1},{x+2,y+2},{x-1,y+3}},f<2 and 'e9eddc' or edge)
  end
  hits[#hits+1]=hit
 end
 save('muzzle-'..kind,muzzle);save('hit-'..kind,hits)
 if kind=='pulse'then save('impact-hull',hits)end
 if kind=='missile'then save('missile-splash',hits);save('boss-blast',hits)end
end
local shield={};local module={}
for f=0,3 do
 local a=image(48,48);local r=8+f*4
 for i=0,2 do local x=24+(i-1)*r*.7;local y=24+((i+f)%2==0 and -r*.4 or r*.4)
  poly(a,{{x,y-6},{x+5,y-3},{x+5,y+3},{x,y+6},{x-5,y+3},{x-5,y-3}},'3d8c9f');poly(a,{{x,y-4},{x+3,y-2},{x+3,y+1},{x,y+3},{x-3,y+1},{x-3,y-2}},'bcf9e8')
 end
 shield[#shield+1]=a
 local m=image(48,48);local d=f*5
 for _,sx in ipairs{-1,1}do for _,sy in ipairs{-1,1}do local x=24+sx*d;local y=24+sy*d
  poly(m,{{x,y},{x+sx*6,y},{x+sx*6,y+sy*4},{x+sx*3,y+sy*6}},f<2 and 'fff0be' or 'b68a63')
 end end
 module[#module+1]=m
end
save('impact-shield',shield);save('impact-module',module)
-- Vòng năng lượng có bề dày và cạnh sáng; glint chạy nhẹ, không nháy thân tàu.
for _,id in ipairs{'death-respawn','shield-activate-break','attack-telegraphs','pickup-collect-upgrade'}do
 local frames={}
 local warning=id=='attack-telegraphs';local reward=id=='pickup-collect-upgrade'
 local edge=warning and '68558f' or reward and '9c713d' or '23576d'
 local mid=warning and 'bd8fdc' or reward and 'edbd69' or '65cddc'
 local light=warning and 'ffe0f8' or reward and 'fff0c5' or 'd2fff3'
 for f=0,3 do
  local a=image(64,64);local radius=reward and (12+f*4) or 25
  for y=1,62 do for x=1,62 do
   local dx,dy=math.abs(x-31.5),math.abs(y-31.5)
   local d=math.max(dx,dy, (dx+dy)*.7)
   if d<=radius and d>=radius-3 then
    local hx=(y<30 and x<43)and light or d>radius-1 and edge or mid
    rect(a,x,y,1,1,hx)
   end
  end end
  local gx=({16,26,37,47})[f+1]
  rect(a,gx,7,4,2,light);rect(a,6,18+f*7,2,4,light)
  if warning then
   for _,x in ipairs{9,49}do for _,y in ipairs{9,49}do rect(a,x,y,6,6,mid);rect(a,x,y,4,2,light)end end
  elseif reward then
   poly(a,{{32,23},{38,32},{32,41},{26,32}},mid);poly(a,{{32,25},{32,36},{28,32}},light)
  end
  frames[#frames+1]=a
 end
 save(id,frames,.1)
 if id=='shield-activate-break' then frames[1]:saveAs(out..'/warden-shield-phase.png')end
end
for _,id in ipairs{'charge-hostile','charge-beam'}do
 local frames={};local special=id=='charge-beam'
 for f=0,3 do
  local a=image(32,32);local r=4+f*2
  disc(a,16,16,r,special and '693f80' or 'a9683e');disc(a,15,15,r-2,special and 'd58fdf' or 'efb978');disc(a,14,14,math.max(1,r-4),'fff5db')
  for _,sign in ipairs{-1,1}do rect(a,16+sign*(r+2)-1,15,3,2,special and 'e9b5ef' or 'ffd19d')end
  frames[#frames+1]=a
 end
 save(id,frames,.08)
end
local core=Image{fromFile=out..'/warden-core.png'};local cores={}
for f=0,3 do local a=Image(core);rect(a,44+f*2,40,3,3,'f4fff5');cores[#cores+1]=a end
save('warden-core-loop',cores,.12)
local crate=Image{fromFile=out..'/legacy-crate.png'};local cargo={}
for f=0,3 do local a=image(96,96);local d=f*7
 for iy=0,1 do for ix=0,1 do
  local piece=image(28,28);piece:drawImage(crate,Point(-ix*28,-iy*28));a:drawImage(piece,Point(20+ix*28+(ix==0 and -d or d),20+iy*28+(iy==0 and -d or d)))
 end end
 if f<2 then poly(a,{{48,34-f*4},{52,44},{62,48},{52,52},{48,63},{44,52},{34,48},{44,44}},'fff0bc')end
 cargo[#cargo+1]=a
end
save('supply-burst',cargo,.08)
-- Workbench rock shatter: giữ canvas và anchor metadata, không auto-center từng frame.
local raw=Image{fromFile=root..'/raw/rock-shatter.png'};local rock={}
for f=0,11 do local a=image(47,43);a:drawImage(raw,Point(-(f%4)*47,-math.floor(f/4)*43));rock[#rock+1]=a end
save('rock-shatter',rock,.071)
print('Coherent projectile, muzzle, impact, cargo and rock effect families authored')
local streak=image(4,48)
for y=1,45 do
 local half=y>34 and 2 or 1
 for x=2-half,1+half do
  local core=x==1 or x==2
  local alpha=math.floor(35+220*(y/45)^.7)
  streak:putPixel(x,y,app.pixelColor.rgba(core and 231 or 96,core and 255 or 207,255,alpha))
 end
end
local s=Sprite(4,48,ColorMode.RGB);s:newCel(s.layers[1],1,streak,Point(0,0));s:saveAs(root..'/speed-streak.aseprite');s:saveAs(out..'/speed-streak.png');s:close()
