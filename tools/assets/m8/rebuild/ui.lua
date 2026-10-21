local root=assert(app.params.root);local out=assert(app.params.out)
local function c(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function rect(im,x,y,w,h,hx)for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and yy>=0 and xx<im.width and yy<im.height then im:putPixel(xx,yy,c(hx))end end end end
local function poly(im,p,hx)
 for y=0,im.height-1 do local xs={}
  for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end
  table.sort(xs);for i=1,#xs-1,2 do rect(im,math.ceil(xs[i]),y,math.floor(xs[i+1])-math.ceil(xs[i])+1,1,hx)end
 end
end
local function save(im,id)
 local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root..'/'..id..'.aseprite');s:saveAs(out..'/'..id..'.png');s:close()
end
local function disc(im,cx,cy,r,hx,inner)
 for y=cy-r,cy+r do for x=cx-r,cx+r do local d=(x-cx)^2+(y-cy)^2;if d<=r*r and(not inner or d>=inner*inner)then rect(im,x,y,1,1,hx)end end end
end
for _,spec in ipairs{{'ui-panel','15263a'},{'ui-card','172c40'},{'ui-modal','172b40'},{'ui-tab','1e3a50'},{'ui-button-secondary','28495c'},{'ui-button','33718b'}}do
 local im=image(32,32)
 poly(im,{{3,0},{28,0},{31,3},{31,28},{28,31},{3,31},{0,28},{0,3}},'537d91')
 poly(im,{{3,1},{28,1},{30,3},{30,28},{28,30},{3,30},{1,28},{1,3}},spec[2])
 rect(im,4,1,24,1,spec[1]=='ui-button' and 'a1e4ed' or '6f95a5')
 rect(im,3,29,26,1,'101e30')
 save(im,spec[1])
end
-- Vỏ rail có độ dày và cạnh vát; border slicing giữ đầu rail khi đổi chiều rộng.
local heat=image(64,16)
poly(heat,{{4,0},{59,0},{63,4},{63,11},{59,15},{4,15},{0,11},{0,4}},'91bac6')
poly(heat,{{4,1},{59,1},{62,4},{62,11},{59,14},{4,14},{1,11},{1,4}},'294455')
poly(heat,{{5,3},{58,3},{60,5},{60,10},{58,12},{5,12},{3,10},{3,5}},'101e30')
rect(heat,6,1,52,1,'d0edf0');rect(heat,6,14,52,1,'14283a');save(heat,'ui-heat-housing')
local fill=image(8,8);rect(fill,0,0,8,8,'d3e5ee');rect(fill,0,0,8,2,'ffffff');rect(fill,0,7,8,1,'8caebd');save(fill,'ui-heat-fill')
local notch=image(2,6);rect(notch,0,0,1,6,'bed2d5');save(notch,'ui-heat-notch')
local function polishedIcon(src)
 local im=image(56,56)
 for y=0,47 do for x=0,47 do local v=src:getPixel(x,y)
  if app.pixelColor.rgbaA(v)>0 then im:putPixel(x+5,y+6,c('142638'))end
 end end
 for y=0,47 do for x=0,47 do local v=src:getPixel(x,y)
  if app.pixelColor.rgbaA(v)>0 then
   local r,g,b=app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v)
   local top=y==0 or app.pixelColor.rgbaA(src:getPixel(x,y-1))==0
   local left=x==0 or app.pixelColor.rgbaA(src:getPixel(x-1,y))==0
   local bottom=y==47 or app.pixelColor.rgbaA(src:getPixel(x,y+1))==0
   if top or left then r=math.min(255,r+35);g=math.min(255,g+35);b=math.min(255,b+30)
   elseif bottom then r=math.floor(r*.72);g=math.floor(g*.72);b=math.floor(b*.78)end
   im:putPixel(x+3,y+3,app.pixelColor.rgba(r,g,b,255))
  end
 end end
 return im
end
for _,kind in ipairs{'pulse','plasma','homing','life','shard','shield','overdrive','support','ammo'}do
 local im=image(48,48)
 if kind=='pulse' then
  for _,x in ipairs{13,27}do poly(im,{{x,9},{x+6,2},{x+6,30},{x,39}},'56d8e5');poly(im,{{x+1,10},{x+4,6},{x+4,29},{x+1,34}},'e8fffc')end
 elseif kind=='plasma' then
  disc(im,24,24,15,'a698db');disc(im,24,24,11,'555493');disc(im,24,24,7,'e9f6ff');rect(im,22,2,3,5,'9dc8ee');rect(im,22,41,3,5,'9dc8ee');rect(im,2,22,5,3,'9dc8ee');rect(im,41,22,5,3,'9dc8ee')
 elseif kind=='ammo' then
  for _,x in ipairs{9,21,33} do
   poly(im,{{x+3,7},{x+6,13},{x+6,35},{x,35},{x,13}},'e6b868')
   rect(im,x+1,15,2,17,'fff0bc');rect(im,x+4,15,2,18,'a67a47');rect(im,x-1,35,8,4,'8da8b5')
  end
  rect(im,7,40,34,3,'d8e7e4')
 elseif kind=='homing' then
  poly(im,{{24,2},{30,12},{30,30},{37,37},{37,42},{28,37},{20,37},{11,42},{11,37},{18,30},{18,12}},'23384b')
  poly(im,{{24,4},{28,12},{28,33},{24,37},{20,33},{20,12}},'deecea')
  poly(im,{{24,4},{28,12},{20,12}},'f0b360');rect(im,20,15,3,16,'ffffff');rect(im,24,15,3,12,'8ca6b5')
  poly(im,{{19,29},{19,36},{13,39}},'8ca6b5');poly(im,{{29,29},{35,39},{29,36}},'8ca6b5');rect(im,22,38,4,6,'eebd6f')
 elseif kind=='life' then
  rect(im,18,5,12,38,'d4e9cb');rect(im,5,18,38,12,'d4e9cb');rect(im,21,8,3,32,'f1fff0');rect(im,8,21,32,3,'f1fff0')
 elseif kind=='shard' then
  poly(im,{{24,2},{40,16},{34,35},{24,46},{11,34},{8,15}},'9b6735');poly(im,{{24,3},{36,16},{24,43},{12,17}},'edbc66');poly(im,{{24,3},{24,42},{12,17}},'fff0be')
 elseif kind=='shield' then
  poly(im,{{24,3},{40,9},{38,28},{32,38},{24,45},{16,38},{10,28},{8,9}},'54bfc5');poly(im,{{24,7},{35,12},{33,27},{28,35},{24,39},{19,34},{15,25},{13,12}},'d2f8ed');poly(im,{{24,10},{24,35},{18,29},{16,14}},'83d8d3')
 elseif kind=='overdrive' then
  poly(im,{{27,2},{11,26},{22,26},{17,46},{38,18},{26,18},{32,2}},'f4c56e');poly(im,{{27,5},{17,23},{24,23},{21,34},{33,21},{24,21}},'fff0be')
 elseif kind=='support' then
  poly(im,{{24,5},{30,19},{41,26},{41,34},{28,29},{24,39},{20,29},{7,34},{7,26},{18,19}},'82d2de');poly(im,{{24,8},{27,20},{24,32},{21,20}},'ebfffa');rect(im,10,17,4,10,'bdd8dd');rect(im,34,17,4,10,'bdd8dd')
 end
 if kind=='shard' then save(Image{fromFile=root..'/../../polish/raw/coin.png'},'ui-shard')
 else save(polishedIcon(im),(kind=='pulse' or kind=='plasma' or kind=='homing') and 'ui-weapon-'..kind or 'ui-'..kind) end
end
