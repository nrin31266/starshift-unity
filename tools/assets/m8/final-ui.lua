-- Batch 2 UI/HUD: icon-first, high contrast.
-- Icon canvas 56x56 (HUD shows 1:1 at 900p), pickup container 64x72, shard giữ 32x40.
local root=assert(app.params.root);local out=assert(app.params.out)
local hex={"080f20","111d32","1c2e46","2d4660","46617a","6e899b","a6bfcb","e5f4f2","0b3948","126578","1b9bb2","41d2de","a0f1ec","271b32","50273d","87354c","bb4057","ed6470","ffb69c","3b2836","745047","ad704b","e8aa58","ffe0a0","282846","505073","8484ac","c0b8d1","464025","7e7745","bab773","f1edb3"}
local pal={};for i,h in ipairs(hex)do pal[i]=app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function put(im,x,y,c)x=math.floor(x);y=math.floor(y);if x>=0 and y>=0 and x<im.width and y<im.height then im:putPixel(x,y,c==0 and 0 or pal[c])end end
local function rect(im,x,y,w,h,c)for yy=y,y+h-1 do for xx=x,x+w-1 do put(im,xx,yy,c)end end end
local function line(im,x,y,xx,yy,c)x=math.floor(x);y=math.floor(y);xx=math.floor(xx);yy=math.floor(yy);local dx,dy=math.abs(xx-x),-math.abs(yy-y);local sx=x<xx and 1 or -1;local sy=y<yy and 1 or -1;local e=dx+dy;while true do put(im,x,y,c);if x==xx and y==yy then break end;local t=e*2;if t>=dy then e=e+dy;x=x+sx end;if t<=dx then e=e+dx;y=y+sy end end end
local function poly(im,p,c)for y=0,im.height-1 do local xs={};for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end;table.sort(xs);for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1])do put(im,x,y,c)end end end end
local function disc(im,cx,cy,rx,ry,c,inner)for y=cy-ry,cy+ry do for x=cx-rx,cx+rx do local d=((x-cx)/rx)^2+((y-cy)/ry)^2;if d<=1 and(not inner or d>=inner)then put(im,x,y,c)end end end end
local function copy(im,src,ox,oy)im:drawImage(src,Point(math.floor(ox),math.floor(oy)))end
local function save(im,id)local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root.."/cleaned/"..id..".aseprite");s:saveAs(out.."/"..id..".png");s:close()end
local function animation(id,w,h,frames)
 local s=Sprite(w,h,ColorMode.RGB);local sheet=image(w*#frames,h)
 for f,a in ipairs(frames)do if f>1 then s:newFrame()end;s:newCel(s.layers[1],f,a,Point(0,0));s.frames[f].duration=.14;copy(sheet,a,(f-1)*w,0)end
 s:saveAs(root.."/cleaned/"..id..".aseprite");s:close();frames[1]:saveAs(out.."/"..id..".png");sheet:saveAs(out.."/"..id.."-sheet.png")
end

-- Emblem 48x48: silhouette đậm chiếm ~36x40, viền navy 2px và lõi sáng.
-- Màu accent lấy theo semantic đã khóa: cyan weapon, teal shield, amber reward/overdrive,
-- green-white life, magenta warning, tím plasma.
local function emblem(kind,size)
 local a=image(size,size);local k=size/48
 local function R(x,y,w,h,v)rect(a,x*k,y*k,w*k,h*k,v)end
 local function P(p,v)local t={};for _,q in ipairs(p)do t[#t+1]={q[1]*k,q[2]*k}end;poly(a,t,v)end
 local function L(x,y,xx,yy,v)line(a,x*k,y*k,xx*k,yy*k,v)end
 local function D(cx,cy,rx,ry,v,inner)disc(a,cx*k,cy*k,rx*k,ry*k,v,inner)end
 if kind=="pulse"then
  -- Pháo năng lượng: thân rộng, buồng nạp phát sáng, hai cánh ổn định.
  P({{24,1},{32,9},{32,16},{39,20},{39,32},{32,35},{32,46},{16,46},{16,35},{9,32},{9,20},{16,16},{16,9}},1)
  P({{24,4},{30,11},{30,17},{36,21},{36,31},{30,34},{30,43},{18,43},{18,34},{12,31},{12,21},{18,17},{18,11}},12)
  P({{24,5},{28,11},{28,18},{33,22},{33,30},{28,33},{28,42},{20,42},{20,33},{15,30},{15,22},{20,18},{20,11}},13)
  R(19,14,10,24,8);R(22,17,4,18,9);R(23,19,2,14,13)
  P({{24,5},{28,10},{20,10}},13)
  R(17,16,2,14,11);R(29,16,2,14,11)
  R(20,38,8,4,9);R(22,41,4,4,11)
 elseif kind=="plasma"then
  -- Quầng plasma: lõi trắng, ring tím sáng, tia ngắn bốn hướng.
  D(24,24,20,20,1);D(24,24,18,18,28);D(24,24,14,14,27);D(24,24,10,10,13);D(24,24,6,6,8)
  L(24,1,24,8,13);L(24,40,24,47,13);L(1,24,8,24,13);L(40,24,47,24,13)
  L(9,9,14,14,13);L(34,34,39,39,13);L(39,9,34,14,13);L(14,34,9,39,13)
  D(21,20,3,3,8);D(24,24,2,2,8)
 elseif kind=="homing"then
  -- Hai tên lửa bệ phóng đôi: đầu cam, thân sáng, lửa đuôi.
  for _,x in ipairs{14,34}do
   P({{x,1},{x+9,8},{x+9,16},{x+12,23},{x+9,29},{x+9,36},{x+5,34},{x,43},{x-5,34},{x-9,36},{x-9,29},{x-12,23},{x-9,16},{x-9,8}},1)
   P({{x,3},{x+7,10},{x+7,17},{x+10,23},{x+7,28},{x+7,35},{x,37},{x-7,35},{x-7,28},{x-10,23},{x-7,17},{x-7,10}},23)
   P({{x,3},{x+6,10},{x,6},{x-6,10}},24)
   R(x-6,10,12,20,6);R(x-6,10,4,20,7)
   R(x-2,12,4,16,8);R(x-6,29,12,3,4);R(x-6,13,3,3,13)
   R(x-2,37,4,6,24);R(x-1,38,2,5,8)
  end
 elseif kind=="ammo"then
  -- Một missile lớn đứng trên ray phóng, đầu cam và flame dưới.
  R(4,38,40,7,1);R(6,39,36,5,6);R(6,39,36,2,7)
  P({{24,1},{34,12},{34,15},{40,21},{40,33},{34,36},{34,41},{14,41},{14,36},{8,33},{8,21},{14,15},{14,12}},1)
  P({{24,4},{32,13},{32,16},{38,22},{38,32},{32,35},{32,39},{16,39},{16,35},{10,32},{10,22},{16,16},{16,13}},6)
  P({{24,4},{30,13},{18,13}},23)
  P({{24,7},{28,14},{20,14}},24)
  R(22,12,4,24,8);R(20,20,8,3,4);R(20,26,8,3,4)
  R(20,42,8,4,23);R(22,44,4,4,24)
  R(9,20,4,12,4);R(35,20,4,12,4);R(10,21,2,10,8);R(36,21,2,10,8)
 elseif kind=="shield"then
  -- Khiên teal sáng, vạch chia giữa và viền cyan trên.
  P({{24,1},{44,9},{42,29},{24,47},{6,29},{4,9}},1)
  P({{24,5},{40,11},{38,28},{24,43},{10,28},{8,11}},12)
  P({{24,9},{36,14},{34,26},{24,38},{14,26},{12,14}},13)
  R(22,12,4,24,8);L(14,14,22,10,8);L(34,14,26,10,8)
  L(13,16,13,26,11);L(35,16,35,26,11)
 elseif kind=="overdrive"then
  -- Tia sét amber lớn, lõi trắng.
  P({{30,1},{6,29},{20,29},{15,47},{42,17},{28,17},{35,1}},1)
  P({{30,4},{10,27},{21,27},{18,42},{38,18},{27,18},{33,4}},23)
  P({{30,6},{14,25},{23,25},{20,36},{34,20},{26,20},{31,6}},24)
  L(30,8,24,24,8);L(24,22,23,31,8)
 elseif kind=="support"then
  -- Drone hỗ trợ: thân trung tâm cyan, hai cánh, ba chân.
  R(3,12,10,10,1);R(35,12,10,10,1);R(5,14,6,6,12);R(37,14,6,6,12)
  P({{24,2},{37,10},{40,26},{30,36},{24,46},{18,36},{8,26},{11,10}},1)
  P({{24,6},{33,12},{36,25},{27,33},{24,39},{21,33},{12,25},{15,12}},13)
  P({{24,11},{31,15},{32,24},{24,30},{16,24},{17,15}},11)
  R(18,14,3,12,8);R(27,14,3,12,8);R(19,32,10,4,11)
  R(5,14,6,2,13);R(37,14,6,2,13)
  R(18,40,12,3,12);R(21,43,6,3,13)
 elseif kind=="life"then
  -- Chữ thập cứu sinh green-white có viền sáng.
  P({{15,2},{33,2},{33,15},{46,15},{46,33},{33,33},{33,46},{15,46},{15,33},{2,33},{2,15},{15,15}},1)
  R(17,4,14,40,31);R(4,17,40,14,31)
  R(19,6,10,36,32);R(6,19,36,10,32)
  R(21,8,6,32,8);R(8,21,32,6,8)
 elseif kind=="shard"then
  -- Tinh thể amber nhiều mặt, cạnh sáng.
  P({{24,1},{43,14},{39,33},{24,47},{9,33},{5,14}},1)
  P({{24,4},{39,15},{36,31},{24,43},{12,31},{9,15}},23)
  P({{24,4},{30,16},{24,43},{18,16}},24)
  P({{24,4},{33,15},{30,16}},22)
  L(9,15,18,16,22);L(39,15,30,16,8);R(22,10,2,10,8)
 elseif kind=="score"then
  -- Ngôi sao tính điểm gold.
  P({{24,1},{31,17},{48,18},{35,29},{39,46},{24,36},{9,46},{13,29},{0,18},{17,17}},1)
  P({{24,5},{30,17},{43,18},{33,27},{36,41},{24,33},{12,41},{15,27},{5,18},{18,17}},23)
  P({{24,9},{28,19},{37,20},{30,26},{32,36},{24,30},{16,36},{18,26},{11,20},{20,19}},24)
  R(23,15,2,12,8)
 elseif kind=="heat"then
  -- Ngọn lửa nhiệt: thân cam, lõi vàng-trắng, đế tối.
  P({{24,1},{34,12},{30,20},{30,30},{34,24},{43,20},{45,33},{38,43},{29,47},{19,47},{10,43},{3,33},{5,20},{14,24},{18,30},{18,20},{14,12}},1)
  P({{24,4},{31,13},{28,21},{28,31},{33,26},{39,21},{41,33},{35,40},{27,43},{21,43},{13,40},{7,33},{9,21},{15,26},{20,31},{20,21},{17,13}},23)
  P({{24,13},{32,26},{29,39},{19,39},{16,26}},24)
  P({{24,20},{28,29},{26,36},{22,36},{20,29}},8)
  R(20,42,8,3,19);R(13,37,4,3,19);R(31,37,4,3,19)
 elseif kind=="pause"then
  -- Hai vạch dày, đủ khối lượng cho touch target 44px.
  P({{10,5},{20,5},{20,39},{10,39}},1);P({{28,5},{38,5},{38,39},{28,39}},1)
  R(11,6,9,34,7);R(29,6,9,34,7)
  R(12,7,5,31,8);R(30,7,5,31,8)
  R(11,34,9,5,6);R(29,34,9,5,6)
  R(12,7,3,3,13);R(30,7,3,3,13)
 elseif kind=="warning"then
  P({{24,2},{47,44},{1,44}},1)
  P({{24,7},{43,41},{5,41}},23)
  P({{24,11},{39,38},{9,38}},24)
  R(22,16,4,14,1);R(22,34,4,4,1)
 end
 return a
end

-- Icon HUD: canvas 48x48, emblem full-bleed để hiển thị 1:1 pixel ở 900p.
local icons={{"ui-weapon-pulse","pulse"},{"ui-weapon-plasma","plasma"},{"ui-weapon-homing","homing"},
 {"ui-ammo","ammo"},{"ui-shield","shield"},{"ui-overdrive","overdrive"},{"ui-support","support"},
 {"ui-life","life"},{"ui-shard","shard"},{"ui-score","score"},{"ui-heat","heat"},
 {"ui-icon-pause","pause"},{"ui-boss-warning","warning"}}
for _,spec in ipairs(icons)do save(emblem(spec[2],48),spec[1]) end

-- Frame/button/card: accent chỉ nằm trong 6px border band hoặc góc, tránh bị nine-slice kéo thành khối.
local function frame(id,plate,inner,edge,top,bottom,corner)
 local a=image(32,32)
 poly(a,{{5,0},{27,0},{31,4},{31,28},{28,31},{4,31},{0,27},{0,5}},1)
 poly(a,{{5,1},{26,1},{30,5},{30,27},{27,30},{5,30},{1,26},{1,5}},edge)
 poly(a,{{6,3},{25,3},{28,6},{28,26},{25,28},{6,28},{3,25},{3,6}},plate)
 poly(a,{{7,5},{24,5},{26,7},{26,24},{23,26},{7,26},{5,23},{5,7}},inner)
 line(a,7,3,25,3,top);line(a,3,7,3,24,top)
 line(a,7,28,25,28,bottom);line(a,28,7,28,24,bottom)
 rect(a,1,1,4,4,corner);rect(a,27,27,4,4,corner)
 return a
end
save(frame("ui-panel",3,2,6,7,1,11),"ui-panel")
save(frame("ui-card",3,2,6,7,1,12),"ui-card")
save(frame("ui-modal",2,1,6,7,1,23),"ui-modal")
save(frame("ui-tab",3,2,5,7,1,11),"ui-tab")
do
 local a=image(32,32)
 poly(a,{{5,0},{27,0},{31,4},{31,28},{28,31},{4,31},{0,27},{0,5}},1)
 poly(a,{{5,1},{26,1},{30,5},{30,27},{27,30},{5,30},{1,26},{1,5}},6)
 poly(a,{{6,3},{25,3},{28,6},{28,26},{25,28},{6,28},{3,25},{3,6}},10)
 poly(a,{{7,5},{24,5},{26,7},{26,23},{22,26},{7,26},{5,23},{5,7}},9)
 line(a,7,3,25,3,13);line(a,3,7,3,24,12)
 line(a,7,28,25,28,9);line(a,28,7,28,24,9)
 rect(a,1,1,5,4,13);rect(a,26,27,5,4,11)
 rect(a,1,4,2,2,12);rect(a,29,26,2,2,12)
 save(a,"ui-button")
end
do
 local a=image(32,32)
 poly(a,{{5,0},{27,0},{31,4},{31,28},{28,31},{4,31},{0,27},{0,5}},1)
 poly(a,{{5,1},{26,1},{30,5},{30,27},{27,30},{5,30},{1,26},{1,5}},5)
 poly(a,{{6,3},{25,3},{28,6},{28,26},{25,28},{6,28},{3,25},{3,6}},2)
 poly(a,{{7,5},{24,5},{26,7},{26,23},{22,26},{7,26},{5,23},{5,7}},3)
 line(a,7,3,25,3,7);line(a,3,7,3,22,6)
 line(a,7,28,25,28,1)
 rect(a,1,1,4,4,12);rect(a,27,27,4,4,10)
 save(a,"ui-button-secondary")
end

-- Heat housing: khung thép sáng (6/7) + bevel cyan (12/13) để nổi trên nền navy.
local a=image(48,24)
poly(a,{{5,0},{43,0},{47,4},{47,20},{43,23},{4,23},{0,19},{0,4}},1)
poly(a,{{5,1},{42,1},{46,5},{46,19},{42,22},{5,22},{1,18},{1,5}},7)
poly(a,{{5,2},{42,2},{45,6},{45,18},{42,21},{6,21},{2,18},{2,6}},6)
poly(a,{{7,5},{40,5},{42,8},{42,16},{40,19},{8,19},{6,16},{6,8}},3)
line(a,6,2,41,2,13);line(a,2,6,2,17,13)
line(a,6,3,41,3,12);line(a,3,8,3,15,11)
line(a,7,20,40,20,4);line(a,45,7,45,16,8)
rect(a,1,8,1,8,13);rect(a,46,8,1,8,13)
rect(a,4,5,3,3,13);rect(a,41,17,3,3,13)
save(a,"ui-heat-housing")
-- Fill gần trắng để tint runtime ra cyan/amber/đỏ đều rực.
a=image(16,16);rect(a,0,0,16,16,7);rect(a,0,0,16,3,8);rect(a,0,3,16,2,13);rect(a,0,13,16,3,6);for x=2,13,4 do rect(a,x,6,1,6,8)end
save(a,"ui-heat-fill")
a=image(8,22);rect(a,0,0,8,22,1);rect(a,1,1,6,20,7);rect(a,2,2,4,18,13);rect(a,3,3,2,16,8);rect(a,2,18,4,3,12)
save(a,"ui-heat-notch")

-- Pickup container: capsule (core/effect) hoặc crate (ammo/support/life), cửa sổ phát sáng.
local function container(variant,kind,accent)
 local im=image(64,72)
 if variant=="capsule"then
  poly(im,{{20,1},{44,1},{57,14},{57,52},{44,71},{20,71},{7,52},{7,14}},1)
  poly(im,{{21,3},{43,3},{55,15},{55,51},{43,69},{21,69},{9,51},{9,15}},5)
  poly(im,{{17,9},{47,9},{51,14},{51,24},{13,24},{13,14}},6)
  line(im,17,9,47,9,7);line(im,9,15,9,50,6)
  for _,x in ipairs{9,51}do rect(im,x,22,4,30,6);rect(im,x+1,24,2,26,7)end
  poly(im,{{14,16},{50,16},{52,21},{52,57},{47,62},{17,62},{12,57},{12,21}},2)
  rect(im,13,17,38,2,11)
  copy(im,emblem(kind,32),16,23)
  rect(im,13,6,4,3,12);rect(im,47,6,4,3,12)
  rect(im,17,63,30,3,accent);rect(im,24,64,16,1,8)
  rect(im,29,66,6,2,accent)
 else
  poly(im,{{13,5},{51,5},{60,15},{60,53},{50,69},{14,69},{4,53},{4,15}},1)
  poly(im,{{14,7},{50,7},{58,16},{58,52},{49,67},{15,67},{6,52},{6,16}},4)
  rect(im,9,10,46,4,6);line(im,9,10,54,10,7)
  for _,x in ipairs{6,51}do rect(im,x,20,6,31,5);rect(im,x+1,22,4,27,6)end
  rect(im,14,17,36,37,1);rect(im,15,18,34,35,2)
  copy(im,emblem(kind,32),16,19)
  rect(im,4,26,6,9,7);rect(im,54,26,6,9,7);rect(im,5,28,3,5,12);rect(im,56,28,3,5,12)
  rect(im,17,64,30,3,accent);rect(im,24,65,16,1,8)
 end
 return im
end
-- Sheen chéo chạy qua container; frame vẫn giữ silhouette và accent riêng.
local function sheen(base,offset)
 local b=Image(base)
 for y=2,69 do
  local cx=offset+math.floor((y-2)*.55)
  for x=0,63 do
   local d=math.abs(x-cx)
   if d<5 then
    local c=b:getPixel(x,y)
    if app.pixelColor.rgbaA(c)>0 then b:putPixel(x,y,pal[d<2 and 8 or 13])end
   end
  end
 end
 for y=10,64,18 do for x=2,61,14 do
  local c=b:getPixel(x,y)
  if app.pixelColor.rgbaA(c)>0 and (x+y)%3==0 then b:putPixel(x,y,pal[8])end
 end end
 return b
end
local pickups={{"pulse-core","pulse","capsule",12},{"plasma-core","plasma","capsule",27},{"homing-core","homing","capsule",23},
 {"shield-cell","shield","capsule",11},{"overdrive","overdrive","capsule",23},
 {"support-drone","support","crate",12},{"missile-ammo","ammo","crate",23},{"extra-life","life","crate",31}}
for _,spec in ipairs(pickups)do
 local base=container(spec[3],spec[2],spec[4])
 local frames={sheen(base,-18),sheen(base,10),sheen(base,38),sheen(base,66)}
 animation(spec[1],64,72,frames)
end
-- Shard giữ canvas native 32x40, chỉ tăng tương phản.
do
 local base=image(32,40);copy(base,emblem("shard",32),0,4)
 local frames={};for f=0,3 do
  local b=Image(base);local y=6+f*7
  for x=0,31 do local d=math.abs(x-16)
   if d<4 then local c=b:getPixel(x,y);if app.pixelColor.rgbaA(c)>0 then b:putPixel(x,y,pal[d<2 and 8 or 24])end end
  end
  rect(b,10,34,12,2,d==nil and 22 or 23)
  frames[#frames+1]=b
 end
 animation("stellar-shard",32,40,frames)
end
print("PASS batch 2 UI, heat housing and pickup containers")
