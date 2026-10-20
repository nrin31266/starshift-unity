local root = app.params.root
assert(root and root ~= "", "Pass --script-param root=/absolute/path/tools/assets/m8")
local out = app.params.out
assert(out and out ~= "", "Pass --script-param out=/absolute/path/app/Assets/StarShift/Art/M8")
local colors = {"080f20","111d32","1c2e46","2d4660","46617a","6e899b","a6bfcb","e5f4f2","0b3948","126578","1b9bb2","41d2de","a0f1ec","271b32","50273d","87354c","bb4057","ed6470","ffb69c","3b2836","745047","ad704b","e8aa58","ffe0a0","282846","505073","8484ac","c0b8d1",464025","7e7745","bab773","f1edb3","16324a","275878","3f87ad","74bcd8","c6ecf7","5c1714","a82b22","e5482f","ff7d55","ffc09e","3c2009","8a4a12","c9751f","f2a333","ffd889","26280b","555614","8e8f24","c6c652","eae9a2","17102b","33205c","5d3aab","9a6ff2","d4bbff","1c2124","353e3d","56645f","92a89b","b6e05a","e4ff9b","0e3a3c","1f7273","47b8b2","9fe7dd","3b566b","6c8ba3","f7c94a","ff3b4e","35e0c8","f2e35a","fffbc0","ff2fd0","ff9ee8"}
local palette = {}
for i, h in ipairs(colors) do palette[i]=app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
local function put(im,x,y,c) if x>=0 and y>=0 and x<im.width and y<im.height then im:putPixel(x,y,c==0 and 0 or palette[c]) end end
local function rect(im,x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do put(im,xx,yy,c) end end end
local function line(im,x0,y0,x1,y1,c)
 local dx,dy=math.abs(x1-x0),-math.abs(y1-y0); local sx=x0<x1 and 1 or -1;local sy=y0<y1 and 1 or -1;local e=dx+dy
 while true do put(im,x0,y0,c);if x0==x1 and y0==y1 then break end;local e2=2*e;if e2>=dy then e=e+dy;x0=x0+sx end;if e2<=dx then e=e+dx;y0=y0+sy end end
end
local function poly(im,points,c)
 for y=0,im.height-1 do
  local xs={};for i=1,#points do local a=points[i];local b=points[i%#points+1];if (a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end
  table.sort(xs);for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1])do put(im,x,y,c)end end
 end
end
local function save(im,name)
 local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root.."/cleaned/"..name..".aseprite");s:saveAs(out.."/"..name..".png");s:close()
end
local function cleanup(name)
 local p=root.."/sources/"..name.."-raw.png";if not app.fs.isFile(p)then return end
 local im=Image{fromFile=p}
 for y=0,im.height-1 do for x=0,im.width-1 do
  local px=im:getPixel(x,y);local a=app.pixelColor.rgbaA(px)
  if a<128 then im:putPixel(x,y,0)else
   local r,g,b=app.pixelColor.rgbaR(px),app.pixelColor.rgbaG(px),app.pixelColor.rgbaB(px);local best,dist=1,1e12
   for i,q in ipairs(palette)do
    local dr,dg,db=r-app.pixelColor.rgbaR(q),g-app.pixelColor.rgbaG(q),b-app.pixelColor.rgbaB(q)
    local d=dr*dr+dg*dg+db*db;if d<dist then best=i;dist=d end
   end
   if name ~= "player" and name ~= "background" and best == 8 then best = 7 end
   im:putPixel(x,y,palette[best])
  end
 end end
 save(im,name)
end
for _,name in ipairs{"player","scout","heavy"}do cleanup(name)end
-- Raider escort dùng lại lõi Scout, thay silhouette thành cánh lưỡi và hai mũi dài.
if app.fs.isFile(out.."/scout.png") then
 local raider=Image(80,80,ColorMode.RGB);local scout=Image{fromFile=out.."/scout.png"}
 for y=0,57 do for x=24,55 do local px=scout:getPixel(x,y);if app.pixelColor.rgbaA(px)>0 then raider:putPixel(x,y+3,px)end end end
 for _,side in ipairs{-1,1}do
  local function wing(points,c)local p={};for _,q in ipairs(points)do p[#p+1]={40+q[1]*side,q[2]}end;poly(raider,p,c)end
  wing({{8,24},{22,25},{34,40},{36,51},{28,57},{15,47},{9,39}},1)
  wing({{10,26},{21,27},{32,41},{33,50},{28,53},{17,45},{11,38}},4)
  wing({{12,28},{20,29},{29,41},{26,45},{18,40}},side<0 and 6 or 5)
  wing({{10,46},{15,50},{14,70},{9,76},{7,67}},1)
  wing({{10,51},{13,52},{12,68},{10,71},{9,66}},3)
  line(raider,40+10*side,54,40+10*side,66,17)
 end
 rect(raider,37,43,6,10,1);rect(raider,38,45,4,7,17);rect(raider,39,45,2,3,19)
 save(raider,"raider")
end
-- Module lấy cụm giáp của heavy làm anchor, ghép lại ở góc overhead thay candidate isometric bị loại.
if app.fs.isFile(out.."/heavy.png") then
 local module=Image(96,96,ColorMode.RGB)
 poly(module,{{30,3},{66,3},{82,15},{86,53},{75,67},{21,67},{10,53},{14,15}},1)
 poly(module,{{31,5},{65,5},{79,17},{82,51},{73,63},{23,63},{14,51},{17,17}},4)
 local h=Image{fromFile=out.."/heavy.png"}
 for y=0,59 do for x=0,55 do local px=h:getPixel(x+36,y+8);if app.pixelColor.rgbaA(px)>0 then module:putPixel(x+20,y+7,px)end end end
 for _,x in ipairs{25,59}do
  rect(module,x,52,12,40,1);rect(module,x+2,54,8,35,3);rect(module,x+2,54,2,32,6)
  rect(module,x+3,72,7,3,5);rect(module,x+3,88,6,3,7);rect(module,x+4,90,4,2,1)
 end
 line(module,18,18,27,9,7);line(module,16,21,16,43,6)
 rect(module,44,47,8,12,1);rect(module,46,49,4,7,17);rect(module,47,50,2,3,19)
 save(module,"boss-module")
end
-- Patch nền giữ phần giữa trống; generated variant đã bị loại sau review.
do
 local bg=Image(768,336,ColorMode.RGB)
 rect(bg,0,0,768,336,1)
 poly(bg,{{0,215},{40,208},{85,221},{122,245},{160,250},{190,272},{236,287},{268,324},{310,336},{0,336}},2)
 poly(bg,{{0,249},{55,241},{99,260},{121,278},{160,281},{190,309},{233,316},{241,336},{0,336}},3)
 poly(bg,{{0,278},{46,268},{74,281},{85,298},{126,306},{151,336},{0,336}},2)
 poly(bg,{{512,0},{548,14},{587,15},{622,35},{650,35},{682,55},{714,53},{754,72},{768,72},{768,0}},2)
 local cx,cy,r=680,61,48
 for y=cy-r,cy+r do for x=cx-r,cx+r do
  local dx,dy=x-cx,y-cy
  if dx*dx+dy*dy<=r*r then
   local c=(dx+dy < -18) and 4 or(dx+dy<16 and 3 or 2)
   put(bg,x,y,c)
  end
 end end
 for y=cy-r,cy+r do for x=cx-r,cx+r do local dx,dy=x-(cx+17),y-(cy+8);if dx*dx+dy*dy<(r-3)*(r-3)then put(bg,x,y,1)end end end
 for i=0,86 do local x=(i*293+97)%768;local y=(i*137+23)%336;put(bg,x,y,i%7==0 and 5 or 3);if i%17==0 then put(bg,x+1,y,3)end end
 for i=0,15 do local x=15+i*17;local y=252+i*3;rect(bg,x,y,2+i%3,1+i%2,2)end
 save(bg,"background")
end
local im=Image(12,32,ColorMode.RGB)
poly(im,{{5,1},{8,5},{8,24},{6,29},{3,24},{3,5}},1);rect(im,4,6,4,19,11);rect(im,5,4,2,21,13);rect(im,5,7,1,16,8);rect(im,4,25,4,3,10);save(im,"player-pulse")
im=Image(20,24,ColorMode.RGB)
poly(im,{{9,1},{17,7},{17,15},{9,23},{2,15},{2,7}},1);poly(im,{{9,3},{15,8},{15,14},{9,21},{4,14},{4,8}},17);poly(im,{{9,5},{13,9},{13,13},{9,18},{6,13},{6,9}},19);rect(im,8,7,3,8,8);save(im,"enemy-bolt")
im=Image(28,28,ColorMode.RGB)
for _,q in ipairs{{2,2,1,1},{25,2,-1,1},{2,25,1,-1},{25,25,-1,-1}} do
 for d=0,6 do put(im,q[1]+d*q[3],q[2],27);put(im,q[1],q[2]+d*q[4],27)end
 for d=1,5 do put(im,q[1]+d*q[3],q[2]+q[4],18);put(im,q[1]+q[3],q[2]+d*q[4],18)end
end
save(im,"enemy-telegraph")
im=Image(32,40,ColorMode.RGB)
poly(im,{{16,2},{27,14},{23,28},{15,37},{6,28},{3,14}},1);poly(im,{{16,5},{25,15},{21,27},{15,34},{8,27},{6,15}},23);poly(im,{{16,5},{16,19},{8,27},{6,15}},24);poly(im,{{16,19},{25,15},{21,27},{15,34}},22);line(im,16,5,16,19,8);line(im,16,5,24,15,24);save(im,"stellar-shard")
im=Image(5,5,ColorMode.RGB);put(im,2,2,7);put(im,1,2,4);put(im,3,2,4);put(im,2,1,4);put(im,2,3,4);save(im,"star")
im=Image(32,32,ColorMode.RGB)
poly(im,{{5,0},{26,0},{31,5},{31,26},{26,31},{5,31},{0,26},{0,5}},1);poly(im,{{5,1},{26,1},{30,5},{30,26},{26,30},{5,30},{1,26},{1,5}},5);poly(im,{{6,3},{25,3},{28,6},{28,25},{25,28},{6,28},{3,25},{3,6}},2);line(im,5,2,25,2,7);line(im,2,5,2,25,4);line(im,5,29,25,29,3);line(im,29,6,29,25,1);save(im,"ui-panel")
im=Image(32,32,ColorMode.RGB)
poly(im,{{5,0},{26,0},{31,5},{31,26},{26,31},{5,31},{0,26},{0,5}},1);poly(im,{{5,1},{26,1},{30,5},{30,26},{26,30},{5,30},{1,26},{1,5}},10);poly(im,{{6,3},{25,3},{28,6},{28,25},{25,28},{6,28},{3,25},{3,6}},9);line(im,5,2,25,2,12);line(im,2,5,2,25,11);rect(im,6,5,3,2,13);rect(im,23,25,3,2,10);save(im,"ui-button")
local explosion=Sprite(64,64,ColorMode.RGB)
for f=0,7 do
 if f>0 then explosion:newFrame()end
 local frame=Image(64,64,ColorMode.RGB)
 if f<7 then
  local radius=({3,8,14,19,22,24,25})[f+1]
  for k=0,7 do
   local angle=k*math.pi/4+.15;local cx=31+math.floor(math.cos(angle)*radius*.55);local cy=31+math.floor(math.sin(angle)*radius*.55)
   local r=math.max(1,math.floor(radius*(f<4 and .7 or .4)))
   poly(frame,{{cx-r,cy-r/2},{cx-r/2,cy-r},{cx+r/2,cy-r},{cx+r,cy-r/2},{cx+r,cy+r/2},{cx+r/2,cy+r},{cx-r/2,cy+r},{cx-r,cy+r/2}},f<3 and 22 or (f<5 and 21 or 3))
   if f<5 then poly(frame,{{cx-r+2,cy-r/2},{cx-r/2,cy-r+2},{cx+r/2-2,cy-r+2},{cx+r-2,cy},{cx,cy+r-2},{cx-r+2,cy+r/2}},f<2 and 8 or(f<4 and 24 or 22))end
   if f>2 then local sx=31+math.floor(math.cos(angle)*radius*1.05);local sy=31+math.floor(math.sin(angle)*radius*1.05);line(frame,sx,sy,sx+math.floor(math.cos(angle)*3),sy+math.floor(math.sin(angle)*3),f<5 and 24 or 22)end
  end
  if f<3 then rect(frame,29-f,28-f,5+f*2,7+f*2,8)end
 end
 explosion:newCel(explosion.layers[1],f+1,frame,Point(0,0));explosion.frames[f+1].duration=.0625
 frame:saveAs(root.."/frames/explosion_"..string.format("%02d",f)..".png")
end
explosion:saveAs(root.."/cleaned/explosion.aseprite");explosion:close()
print("PASS: native pixel assets authored; no resize/trim")
