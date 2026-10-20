local root=assert(app.params.root);local out=assert(app.params.out)
local hex={"080f20","111d32","1c2e46","2d4660","46617a","6e899b","a6bfcb","e5f4f2","0b3948","126578","1b9bb2","41d2de","a0f1ec","271b32","50273d","87354c","bb4057","ed6470","ffb69c","3b2836","745047","ad704b","e8aa58","ffe0a0","282846","505073","8484ac","c0b8d1","464025","7e7745","bab773","f1edb3","16324a","275878","3f87ad","74bcd8","c6ecf7","5c1714","a82b22","e5482f","ff7d55","ffc09e","3c2009","8a4a12","c9751f","f2a333","ffd889","26280b","555614","8e8f24","c6c652","eae9a2","17102b","33205c","5d3aab","9a6ff2","d4bbff","1c2124","353e3d","56645f","92a89b","b6e05a","e4ff9b","0e3a3c","1f7273","47b8b2","9fe7dd","3b566b","6c8ba3","f7c94a","ff3b4e","35e0c8","f2e35a","fffbc0","ff2fd0","ff9ee8"}
local pal={};for i,h in ipairs(hex)do pal[i]=app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function put(im,x,y,c)x=math.floor(x);y=math.floor(y);if x>=0 and y>=0 and x<im.width and y<im.height then im:putPixel(x,y,c==0 and 0 or pal[c])end end
local function rect(im,x,y,w,h,c)for yy=y,y+h-1 do for xx=x,x+w-1 do put(im,xx,yy,c)end end end
local function line(im,x,y,xx,yy,c)x=math.floor(x);y=math.floor(y);xx=math.floor(xx);yy=math.floor(yy);local dx,dy=math.abs(xx-x),-math.abs(yy-y);local sx=x<xx and 1 or -1;local sy=y<yy and 1 or -1;local e=dx+dy;while true do put(im,x,y,c);if x==xx and y==yy then break end;local t=e*2;if t>=dy then e=e+dy;x=x+sx end;if t<=dx then e=e+dx;y=y+sy end end end
local function poly(im,p,c)for y=0,im.height-1 do local xs={};for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end;table.sort(xs);for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1])do put(im,x,y,c)end end end end
local function disc(im,cx,cy,rx,ry,c,inner)for y=cy-ry,cy+ry do for x=cx-rx,cx+rx do local d=((x-cx)/rx)^2+((y-cy)/ry)^2;if d<=1 and(not inner or d>=inner)then put(im,x,y,c)end end end end
local function copy(im,src,ox,oy)im:drawImage(src,Point(ox or 0,oy or 0))end
local function load(id)return Image{fromFile=out.."/"..id..".png"}end
local function save(im,id)local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root.."/cleaned/"..id..".aseprite");s:saveAs(out.."/"..id..".png");s:close()end
local function normalize(id)
 local im=Image{fromFile=root.."/sources/"..id.."-raw.png"}
 for px in im:pixels()do local v=px();if app.pixelColor.rgbaA(v)<128 then px(0)else local r,g,b=app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v);local best,d=1,1e12;for i,q in ipairs(pal)do local dd=(r-app.pixelColor.rgbaR(q))^2+(g-app.pixelColor.rgbaG(q))^2+(b-app.pixelColor.rgbaB(q))^2;if dd<d then best=i;d=dd end end;if id=="interceptor" and best>=9 and best<=13 then best=({25,26,27,27,28})[best-8]end;if id=="supply-drone" and best>=9 and best<=13 then best=({20,21,22,23,24})[best-8]end;px(pal[best])end end
 save(im,id)
end
for _,id in ipairs{"player-swept","bulwark","sniper","interceptor","carrier","supply-drone","vanguard-hull","warden-hull","asteroid-anchor"}do normalize(id)end
-- Các vết nứt chỉ thay pixel đã có trong giáp; canvas và silhouette giữ nguyên.
for _,id in ipairs{"scout","raider","heavy","bulwark","sniper","interceptor","carrier","warden-drone","vanguard-hull","warden-hull"}do
 if id=="warden-drone"then local im=image(48,48);local src=load("scout");local small=Image(src);small:resize(48,38);copy(im,small,0,5);save(im,id)end
 local src=load(id)
 for stage=1,2 do local im=Image(src);for k=1,stage*3 do local cx=math.floor(im.width*(.25+.13*k)% (im.width-12))+6;local cy=math.floor(im.height*(.21+.17*k)% (im.height-16))+8;for n=0,8+stage*3 do local x=cx+math.floor(n*.45);local y=cy+n; if x<im.width and y<im.height and app.pixelColor.rgbaA(src:getPixel(x,y))>0 then put(im,x,y,1);if x+1<im.width and app.pixelColor.rgbaA(src:getPixel(x+1,y))>0 then put(im,x+1,y,n%4==0 and 23 or 21)end end end end;local name=id:gsub("%-hull$","")..(stage==1 and "-damaged"or"-critical");save(im,name)end
end
local function plate(w,h,accent,kind)
 local im=image(w,h);poly(im,{{8,2},{w-9,2},{w-3,10},{w-3,h-12},{w-12,h-3},{11,h-3},{2,h-12},{2,10}},1);poly(im,{{9,4},{w-10,4},{w-5,11},{w-5,h-13},{w-13,h-5},{12,h-5},{4,h-13},{4,11}},4);poly(im,{{12,7},{w-13,7},{w-9,12},{w-9,h-18},{9,h-18},{9,12}},5);line(im,12,7,w-14,7,7);line(im,8,13,8,h-19,6)
 -- Tái dùng các cụm giáp heavy thay cho mặt phẳng trống trên module.
 local material=load("heavy");for y=14,math.floor(h*.49)do for x=12,w-13 do local sx=math.floor((x-12)/(w-24)*80)+24;local sy=math.floor((y-14)/math.max(1,h*.49-14)*32)+18;local px=material:getPixel(sx,sy);if app.pixelColor.rgbaA(px)>0 then im:putPixel(x,y,px)end end end
 for x=16,w-16,12 do rect(im,x,12,5,2,3);rect(im,x,h-16,5,2,accent)end
 if kind=="engine"then for x=14,w-15,14 do rect(im,x,h-28,10,22,1);rect(im,x+2,h-23,6,12,accent);rect(im,x+3,h-22,2,8,8)end
 elseif kind=="core"then disc(im,w/2,h/2,w*.23,h*.23,1);disc(im,w/2,h/2,w*.19,h*.19,accent);disc(im,w/2,h/2,w*.11,h*.11,24);disc(im,w/2-3,h/2-3,3,3,8)
 elseif kind=="missile"then for x=18,w-20,16 do for y=24,h-25,18 do rect(im,x,y,10,13,1);poly(im,{{x+5,y+2},{x+8,y+7},{x+8,y+10},{x+2,y+10},{x+2,y+7}},accent)end end
 else for x=math.floor(w*.3),math.floor(w*.7),math.floor(w*.4)do rect(im,x-5,h*.4,12,h*.56,1);rect(im,x-3,h*.4,8,h*.52,3);rect(im,x-3,h*.42,2,h*.47,7);rect(im,x-2,h-10,6,3,accent)end end;return im
end
for _,family in ipairs{"vanguard","warden"}do
 local accent=family=="vanguard"and 17 or 27
 local specs=family=="vanguard" and {{"engine","engine"},{"reactor","core"},{"missile-pod","missile"},{"beam-pod","gun"}} or {{"core","core"},{"beam-node","gun"},{"drone-node","missile"},{"plasma-node","core"}}
 for _,spec in ipairs(specs)do save(plate(96,96,accent,spec[2]),family.."-"..spec[1])end
 local wing=image(256,112);poly(wing,{{2,35},{62,6},{180,9},{253,40},{240,92},{167,107},{60,87},{2,65}},1);poly(wing,{{5,37},{64,9},{178,12},{248,42},{236,88},{166,102},{62,83},{5,62}},4);poly(wing,{{9,38},{64,12},{171,15},{193,32},{67,37},{13,56}},6);line(wing,64,12,170,15,7);for x=50,210,24 do line(wing,x,43,x+19,82,3);line(wing,x+1,43,x+20,82,5);rect(wing,x,66,6,3,accent)end;save(wing,family.."-wing")
end
-- Biến thể đá dùng canvas native, cùng anchor và các dải sắc độ.
for _,spec in ipairs{{"small",48},{"medium",64},{"large",96},{"huge",128}}do local im=load("asteroid-anchor");im:resize(spec[2],spec[2]);save(im,"asteroid-"..spec[1])end
local rock=load("asteroid-large");for k=0,4 do line(rock,28+k*6,20+k*10,35+k*6,35+k*10,23);line(rock,29+k*6,20+k*10,36+k*6,35+k*10,24)end;save(rock,"resource-asteroid")
local im=image(24,24);poly(im,{{12,1},{21,8},{19,18},{12,23},{4,18},{2,8}},1);poly(im,{{12,3},{18,9},{16,17},{12,20},{7,17},{5,9}},26);poly(im,{{12,5},{16,10},{14,16},{10,17},{8,9}},28);rect(im,10,7,3,9,8);save(im,"player-plasma")
for _,s in ipairs{{"player-homing",16,32},{"reserve-missile",20,40}}do local id,w,h=table.unpack(s);im=image(w,h);poly(im,{{w/2,1},{w*.75,8},{w*.75,h*.7},{w-1,h-6},{w*.65,h-8},{w*.6,h-2},{w*.4,h-2},{w*.35,h-8},{1,h-6},{w*.25,h*.7},{w*.25,8}},1);rect(im,w*.3,9,w*.4,h*.53,7);line(im,w*.35,10,w*.35,h*.68,8);poly(im,{{w/2,3},{w*.65,9},{w*.35,9}},23);rect(im,w*.4,h-9,w*.2,6,23);save(im,id)end
im=image(40,12);poly(im,{{1,6},{7,1},{33,1},{39,6},{33,10},{7,10}},1);rect(im,7,3,26,6,17);rect(im,10,4,20,3,19);rect(im,14,5,12,1,8);save(im,"boss-curtain")
local pickupIds={"pulse-core","plasma-core","homing-core","shield-cell","overdrive","support-drone","missile-ammo","extra-life","stellar-shard"}
local accents={12,27,23,11,23,12,23,31,23}
for index,id in ipairs(pickupIds)do
 local base=image(32,40);local c=accents[index]
 if id=="stellar-shard"then base=load(id)else
 poly(base,{{8,3},{23,3},{29,11},{29,29},{23,36},{8,36},{2,29},{2,11}},1);poly(base,{{9,5},{22,5},{27,12},{27,28},{22,34},{9,34},{4,28},{4,12}},4);line(base,9,6,21,6,7);line(base,5,12,5,26,6);rect(base,9,10,14,19,2);rect(base,9,31,14,2,c)
 if index==1 then poly(base,{{16,10},{20,17},{17,20},{17,27},{13,27},{13,20},{11,17}},c);rect(base,15,12,2,13,8)
 elseif index==2 then disc(base,16,19,8,8,c,.55);disc(base,16,19,3,3,28)
 elseif index==3 or index==7 then poly(base,{{16,10},{20,16},{20,24},{23,27},{16,25},{9,27},{12,23},{12,16}},c);rect(base,15,14,2,8,8)
 elseif index==4 then poly(base,{{16,10},{24,13},{22,22},{16,28},{9,22},{8,13}},c);poly(base,{{16,13},{21,15},{19,21},{16,24},{12,20},{11,15}},13)
 elseif index==5 then poly(base,{{18,9},{11,20},{16,20},{13,29},{23,17},{18,17}},c);line(base,18,10,13,18,8)
 elseif index==6 then poly(base,{{16,13},{24,23},{19,22},{16,26},{13,22},{8,23}},c);rect(base,15,15,2,7,8)
 elseif index==8 then rect(base,13,11,6,17,c);rect(base,8,17,16,5,c);rect(base,14,12,2,14,32)end end
 local frames={};for f=0,3 do local a=Image(base);local x=8+f*5;local y=10+f*5;line(a,x-2,y,x+2,y,8);line(a,x,y-2,x,y+2,24);frames[#frames+1]=a end
 local s=Sprite(32,40,ColorMode.RGB);local sheet=image(128,40);for f,a in ipairs(frames)do if f>1 then s:newFrame()end;s:newCel(s.layers[1],f,a,Point(0,0));s.frames[f].duration=.14;copy(sheet,a,(f-1)*32,0)end;s:saveAs(root.."/cleaned/"..id..".aseprite");s:close();frames[1]:saveAs(out.."/"..id..".png");sheet:saveAs(out.."/"..id.."-sheet.png")
end
-- HUD icon/pickup container do final-ui.lua sở hữu (canvas 48px), không generate ở đây.
local drone=image(32,32);poly(drone,{{16,3},{22,13},{30,23},{21,21},{16,28},{11,21},{2,23},{10,13}},1);poly(drone,{{16,6},{20,15},{27,21},{20,19},{16,25},{12,19},{5,21},{12,15}},7);rect(drone,14,12,4,9,12);line(drone,15,12,15,18,8);save(drone,"support-companion")
for _,spec in ipairs{{"engine-loop",32,40,12},{"muzzle-flash",32,32,12},{"hit-sparks",32,32,23},{"smoke-debris",64,64,6},{"missile-splash",64,64,23},{"shield-activate-break",64,64,11},{"pickup-collect-upgrade",48,48,23},{"death-respawn",64,80,11},{"boost-warp",64,64,12},{"attack-telegraphs",64,64,27},{"warden-shield-phase",128,128,11}}do
 local id,w,h,c=table.unpack(spec);local sheet=image(w*4,h);local s=Sprite(w,h,ColorMode.RGB)
 for f=0,3 do local a=image(w,h);if id=="engine-loop"then poly(a,{{w/2,2},{w*.8,9},{w*.65,23+f*3},{w/2,h-2-f*2},{w*.35,23+f*3},{w*.2,9}},10);poly(a,{{w/2,4},{w*.64,10},{w/2,29+f},{w*.36,10}},c);rect(a,w/2-1,6,2,13+f*2,8)
 elseif id=="smoke-debris"then for k=0,5 do local t=k*math.pi/3+.4;local cx=w/2+math.cos(t)*(8+f*4);local cy=h/2+math.sin(t)*(8+f*4);disc(a,cx,cy,4+f,3+f,f<2 and 4 or 3);line(a,cx,cy,cx+3,cy-2,6)end
 elseif id=="missile-splash"then for k=0,7 do local t=k*math.pi/4;local cx=w/2+math.cos(t)*(6+f*5);local cy=h/2+math.sin(t)*(6+f*5);disc(a,cx,cy,6-f,6-f,f<2 and 23 or 21);disc(a,cx-1,cy-1,3,3,f<2 and 24 or 22)end
 elseif id=="muzzle-flash"then for k=0,7 do local t=k*math.pi/4;line(a,w/2,h/2,w/2+math.cos(t)*(12-f*2),h/2+math.sin(t)*(12-f*2),c)end;disc(a,w/2,h/2,4-f*.5,4-f*.5,8)
 else local radius=math.floor(math.min(w,h)*(.2+f*.075));disc(a,w/2,h/2,radius,radius,c,.88);for k=0,7 do local t=k*math.pi/4+.3;local x=w/2+math.cos(t)*(radius+2);local y=h/2+math.sin(t)*(radius+2);line(a,x,y,x+math.cos(t)*3,y+math.sin(t)*3,f<2 and 8 or c)end end
 if f>0 then s:newFrame()end;s:newCel(s.layers[1],f+1,a,Point(0,0));s.frames[f+1].duration=.09;copy(sheet,a,f*w,0);if f==0 then a:saveAs(out.."/"..id..".png")end end
 s:saveAs(root.."/cleaned/"..id..".aseprite");s:close();sheet:saveAs(out.."/"..id.."-sheet.png")
end
im=image(28,28);for _,x in ipairs{3,22}do rect(im,x,6,3,16,23);rect(im,x+1,7,1,12,24)end;rect(im,7,2,14,2,23);rect(im,7,24,14,2,23);save(im,"elite-accent")
for _,id in ipairs{"astra-planet","crimson-planet"}do im=image(256,256);local warm=id=="crimson-planet";disc(im,128,128,121,121,1);disc(im,126,126,117,117,warm and 15 or 3);disc(im,117,119,105,109,warm and 16 or 4);for y=35,212 do for x=28,215 do local dx,dy=(x-126)/115,(y-126)/115;if dx*dx+dy*dy<.88 and math.sin(x*.047+math.sin(y*.065)*1.6)+math.cos(y*.09)>.5 then put(im,x,y,warm and 20 or 9)end end end;disc(im,179,154,94,103,2);disc(im,195,160,83,100,1);save(im,id)end
im=load("background");for p in im:pixels()do local v=p();if v==pal[3]then p(pal[14])elseif v==pal[4]then p(pal[15])end end;save(im,"crimson-background")
im=load("asteroid-anchor");for p in im:pixels()do local v=p();if app.pixelColor.rgbaA(v)>0 then local r=app.pixelColor.rgbaR(v);p(r>100 and pal[4]or pal[2])end end;save(im,"parallax-debris")
im=plate(32,40,23,"missile");save(im,"legacy-crate")
print("PASS production source/export batch")
-- Core động tách riêng để không ghi đè hull khi damage state đổi.
do
 local base=load("warden-core");local sheet=image(384,96);local s=Sprite(96,96,ColorMode.RGB)
 for f=0,3 do local a=Image(base);disc(a,48,48,11+f,11+f,f%2==0 and 27 or 28,.8);if f>0 then s:newFrame()end;s:newCel(s.layers[1],f+1,a,Point(0,0));s.frames[f+1].duration=.16;copy(sheet,a,f*96,0)end;s:saveAs(root.."/cleaned/warden-core-loop.aseprite");s:close();sheet:saveAs(out.."/warden-core-loop-sheet.png")
end
-- Cửa hangar chỉ phát chuyển động khi EscortSpawner đã phóng drone thật.
do
 local sheet=image(256,32);local s=Sprite(64,32,ColorMode.RGB)
 for f=0,3 do local a=image(64,32);poly(a,{{5,2},{58,2},{62,6},{62,26},{58,30},{5,30},{1,26},{1,6}},1);rect(a,5,5,54,22,3);rect(a,8,8,48,17,1);line(a,6,5,57,5,6)
 for x=10,52,7 do rect(a,x,22,3,1,23)end
 local door=math.floor(23*(1-f/3));if door>0 then rect(a,8,8,door,13,4);rect(a,56-door,8,door,13,5);for x=10,54,7 do if x<8+door or x>56-door then line(a,x,9,x,18,6)end end end
 if f>0 then s:newFrame()end;s:newCel(s.layers[1],f+1,a,Point(0,0));s.frames[f+1].duration=.1;copy(sheet,a,f*64,0);if f==0 then a:saveAs(out.."/carrier-bay-loop.png")end end
 s:saveAs(root.."/cleaned/carrier-bay-loop.aseprite");s:close();sheet:saveAs(out.."/carrier-bay-loop-sheet.png")
end
-- Khung giằng nối hull và module tại mount gameplay, không kéo giãn một hull duy nhất.
for _,id in ipairs{"vanguard-truss","warden-truss"}do
 local a=image(192,48);local accent=id=="vanguard-truss"and 17 or 27
 poly(a,{{2,14},{17,7},{174,7},{189,14},{189,34},{174,41},{17,41},{2,34}},1);rect(a,9,13,174,22,3);rect(a,10,13,172,3,6);rect(a,10,32,172,2,4)
 for x=17,168,24 do poly(a,{{x,16},{x+7,16},{x+22,30},{x+15,30}},5);line(a,x+2,17,x+17,30,7);rect(a,x+10,22,4,2,accent)end
 rect(a,4,17,9,14,4);rect(a,179,17,9,14,4);line(a,5,17,11,17,7);save(a,id)
end
-- Nebula là các cụm kín, không chạm mép patch; planet dùng renderer riêng.
for _,id in ipairs{"background","crimson-background"}do
 local a=image(768,336);rect(a,0,0,768,336,1);local warm=id=="crimson-background"
 poly(a,{{32,240},{58,213},{95,209},{128,221},{170,229},{200,251},{245,255},{283,275},{325,279},{376,309},{315,315},{267,304},{209,300},{166,282},{122,277},{87,264},{52,263}},warm and 14 or 2)
 poly(a,{{64,239},{91,226},{121,237},{170,247},{190,264},{238,269},{270,286},{310,294},{256,290},{208,283},{161,266},{112,259}},warm and 15 or 3)
 poly(a,{{430,30},{480,22},{530,34},{580,33},{620,46},{690,53},{725,72},{682,78},{632,65},{585,62},{550,51},{500,50},{466,37}},warm and 14 or 2)
 for i=0,86 do local x=16+(i*293+97)%736;local y=16+(i*137+23)%304;put(a,x,y,i%7==0 and 5 or 3)end
 save(a,id)
end
