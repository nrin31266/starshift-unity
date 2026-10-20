-- Batch 3: 7 armor families, damage states, hostile/friendly projectiles, destruction debris.
-- Chạy SAU production.lua và final-player.lua: script này retint hull gốc sang family palette,
-- author damage state thật (mất mảng giáp, lộ internals) và các effect sheet mới.
-- Không đổi silhouette gốc ở state intact; damage state chỉ ăn vào biên silhouette.
local root=assert(app.params.root);local out=assert(app.params.out)

-- 32 swatch gốc dùng cho normalize nguồn; phần append là family ramps của Batch 3.
local baseHex={"080f20","111d32","1c2e46","2d4660","46617a","6e899b","a6bfcb","e5f4f2","0b3948","126578","1b9bb2","41d2de","a0f1ec","271b32","50273d","87354c","bb4057","ed6470","ffb69c","3b2836","745047","ad704b","e8aa58","ffe0a0","282846","505073","8484ac","c0b8d1","464025","7e7745","bab773","f1edb3"}
local function family(r,a,g)return{ramp=r,accent=a,glow=g}end
local families={
 -- Scout steel/cyan: hull thép lạnh, cyan emissive.
 scout=family({"17384f","23526c","2b6685","4ea3c4","93d5e8","d8f5fb"},
   {"0a6b7a","18c2d4","8ff2f7","d6fbff"},{"18c2d4","8ff2f7","d6fbff"}),
 -- Raider navy/coral.
 raider=family({"101f36","182e49","20395c","3f6390","7fa3c9","c6dcf0"},
   {"7a1f1a","e5482f","ffb08c","ffe0d2"},{"e5482f","ffb08c","ffe0d2"}),
 -- Bomber bronze/orange.
 heavy=family({"3a2410","573314","6f441b","a96a26","dda24c","f7d79b"},
   {"7a2a08","e8641c","ff8a1e","ffd889"},{"e8641c","ff8a1e","ffd889"}),
 -- Bulwark olive/cream.
 bulwark=family({"2b2f12","3f4419","525a22","8b9238","c3c86e","eeedb6"},
   {"5a4210","c99a2e","f0d07a","fff4cc"},{"c99a2e","f0d07a","fff4cc"}),
 -- Sniper violet/indigo.
 sniper=family({"1b1436","281d4e","352866","5c47a6","9b83de","d8c9ff"},
   {"3b1f7a","7a52e0","c9b2ff","ebe2ff"},{"7a52e0","c9b2ff","ebe2ff"}),
 -- Interceptor graphite/lime.
 interceptor=family({"1a2222","272f2f","333f3e","566864","92a79b","d3e0d6"},
   {"4a5a12","a9c62a","e4ff9b","f6ffd0"},{"a9c62a","e4ff9b","f6ffd0"}),
 -- Carrier teal/blue-gray.
 carrier=family({"0e3035","134247","195359","2f8a8c","79c9c2","c9efe9"},
   {"2b4a5e","5a8299","a8c8da","e2f2f6"},{"2f8a8c","79c9c2","c9efe9"}),
 -- Supply Drone ivory/amber.
 supply=family({"3a3123","544834","6e6046","a99b78","ded3b4","f7f1de"},
   {"8a5a10","f0b030","ffe2a0","fff7d8"},{"f0b030","ffe2a0","fff7d8"}),
 -- Vanguard: steel hull, crimson armor, gold reactor (palette riêng).
 vanguard=family({"1d1722","2b2132","3a2a3f","5f4a66","9b86a6","d8c9dc"},
   {"8f1d2c","d8323f","ff8a7a","ffd0c4"},{"f2b13a","ffdc8a","fff4d0"}),
 -- Warden: indigo hull, teal energy (palette riêng).
 warden=family({"171331","211a44","2c2357","4b3d8c","8a7bc9","cfc6f2"},
   {"0f5b5f","22a3a5","8ff0e6","d8fff8"},{"35e0c8","8ff0e6","d8fff8"})
}
local effects={"ff6a4a","ffe98a","fff6c8","ff2fd0","ff9ee8","ffd0f0","f2f7ff","fff3b0","1b1f22","3a4348","6b7578","1c2124","353e3d","56645f","666f74","92a89b","8a4a12","c9751f","f2a333","ff7d55","ffffff","fffbc0","3a3010","e8d24a","f7c94a","fff3b0","ffd0f0","d4bbff","ebe2ff","5c1714","271b32","0b3948","1b9bb2","a0f1ec","3a3010","e8d24a","fffbc0","e5482f","ffb69c","7a52e0","3a4348"}
local hex={};local seen={}
for _,list in ipairs{baseHex,effects}do for _,h in ipairs(list)do if not seen[h]then seen[h]=true;hex[#hex+1]=h end end end
for _,name in ipairs{"scout","raider","heavy","bulwark","sniper","interceptor","carrier","supply","vanguard","warden"}do
 local f=families[name]
 for _,list in ipairs{f.ramp,f.accent,f.glow}do for _,h in ipairs(list)do if not seen[h]then seen[h]=true;hex[#hex+1]=h end end end
end
local pal={};local index={}
for i,h in ipairs(hex)do pal[i]=app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255);index[h]=i end
local function c(h)return index[h]end
for name,f in pairs(families)do
 -- Clamp: mọi truy cập ramp/accent/glow đều an toàn khi family ngắn hơn map value.
 local function ramp(list)
  local out={};for i,h in ipairs(list)do out[i]=c(h)end
  for i=1,8 do if out[i]==nil then out[i]=out[math.min(#list,i)]end end
  return out
 end
 f.rampIdx=ramp(f.ramp);f.accentIdx=ramp(f.accent);f.glowIdx=ramp(f.glow)
end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function put(im,x,y,v)x=math.floor(x);y=math.floor(y);if x>=0 and y>=0 and x<im.width and y<im.height then im:putPixel(x,y,v==0 and 0 or pal[v])end end
local function rect(im,x,y,w,h,v)for yy=y,y+h-1 do for xx=x,x+w-1 do put(im,xx,yy,v)end end end
local function line(im,x,y,xx,yy,v)x=math.floor(x);y=math.floor(y);xx=math.floor(xx);yy=math.floor(yy);local dx,dy=math.abs(xx-x),-math.abs(yy-y);local sx=x<xx and 1 or -1;local sy=y<yy and 1 or -1;local e=dx+dy;while true do put(im,x,y,v);if x==xx and y==yy then break end;local t=e*2;if t>=dy then e=e+dy;x=x+sx end;if t<=dx then e=e+dx;y=y+sy end end end
local function poly(im,p,v)for y=0,im.height-1 do local xs={};for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end;table.sort(xs);for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1])do put(im,x,y,v)end end end end
local function disc(im,cx,cy,rx,ry,v,inner)for y=cy-ry,cy+ry do for x=cx-rx,cx+rx do local d=((x-cx)/rx)^2+((y-cy)/ry)^2;if d<=1 and(not inner or d>=inner)then put(im,x,y,v)end end end end
-- Base snapshot giữ hull gốc trước retint, để chạy lại batch không bị nhân đôi hiệu ứng.
local function basePath(id)
 local snapshot=root.."/base/"..id..".png"
 if app.fs.isFile(snapshot)then return snapshot end
 return out.."/"..id..".png"
end
local function load(id)return Image{fromFile=basePath(id)}end
local function has(id)return app.fs.isFile(basePath(id))end
local function save(im,id)local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root.."/cleaned/"..id..".aseprite");s:saveAs(out.."/"..id..".png");s:close()end
local function saveSheet(id,w,h,frames,seconds)
 local s=Sprite(w,h,ColorMode.RGB);local sheet=image(w*#frames,h)
 for f,a in ipairs(frames)do if f>1 then s:newFrame()end;s:newCel(s.layers[1],f,a,Point(0,0));s.frames[f].duration=seconds or .09;sheet:drawImage(a,Point((f-1)*w,0))end
 s:saveAs(root.."/cleaned/"..id..".aseprite");s:close();frames[1]:saveAs(out.."/"..id..".png");sheet:saveAs(out.."/"..id.."-sheet.png")
end
local function alpha(im,x,y)if x<0 or y<0 or x>=im.width or y>=im.height then return 0 end return app.pixelColor.rgbaA(im:getPixel(x,y))end
local function opaque(im,x,y)return alpha(im,x,y)>0 end
local function clear(im,x,y)if x>=0 and y>=0 and x<im.width and y<im.height then im:putPixel(x,y,0)end end
local function paletteIndex(im,x,y)local v=im:getPixel(x,y);for i,q in ipairs(pal)do if v==q then return i end end return 0 end

-- Retint: map hull theo value band, accent/glow theo family. 1 giữ navy outline chung.
local neutral={[2]=1,[3]=1,[4]=2,[5]=3,[6]=4,[7]=5,[8]=6,[25]=1,[26]=2,[27]=4,[28]=6,[20]=1,[21]=3,[22]=5,[29]=1,[30]=3,[31]=5,[32]=6}
local accentMap={[14]=1,[15]=1,[16]=2,[17]=3,[18]=3,[19]=4,[23]=2,[24]=4}
local glowMap={[9]=1,[10]=1,[11]=2,[12]=3,[13]=4}
local function retint(im,f)
 local result=Image(im.width,im.height)
 for y=0,im.height-1 do for x=0,im.width-1 do
  local v=im:getPixel(x,y);if app.pixelColor.rgbaA(v)==0 then result:putPixel(x,y,0)else
   local old=paletteIndex(im,x,y);local target=1
   if neutral[old]then target=f.rampIdx[neutral[old]]
   elseif accentMap[old]then target=f.accentIdx[accentMap[old]]
   elseif glowMap[old]then target=f.glowIdx[glowMap[old]]end
   result:putPixel(x,y,pal[target])end
 end end
 return result
end

-- Damage state: bite mở từ biên silhouette, lộ internals, rìa rách, vết nứt và sparks.
local function bbox(im)
 local minx,maxx,miny,maxy=im.width,-1,im.height,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if opaque(im,x,y)then if x<minx then minx=x end;if x>maxx then maxx=x end;if y<miny then miny=y end;if y>maxy then maxy=y end end end end
 return minx,miny,maxx,maxy
end
local function edgeX(im,y0,y1,fromLeft)
 local best=fromLeft and im.width or -1
 for y=y0,y1 do for x=0,im.width-1 do if opaque(im,x,y)then if fromLeft then if x<best then best=x end elseif x>best then best=x end;break end end end
 if best==(fromLeft and im.width or -1)then return nil end
 return best
end
local function jag(x,y,seed)
 local n=math.sin(x*1.7+y*2.3+seed*3.1)*.5+math.sin(x*.43-y*1.9+seed)*.5
 return n
end
-- Cắt một mảng giáp theo đa giác răng cưa: phần ngoài silhouette thành trong suốt,
-- phần trong hull lộ khoang máy. Mép cắt có highlight kim loại, không rải noise.
local function bite(im,f,cx,cy,rx,ry,seed,deep)
 -- Đa giác rách 11 đỉnh, bán kính dao động .78-1.12; mép ngoài có mảnh vỏ lồi.
 local points={};local count=11
 for i=1,count do
  local t=(i-1)/count*math.pi*2
  local wobble=1+jag(math.cos(t)*5,math.sin(t)*5,seed)*(deep and .24 or .16)
  points[#points+1]={cx+math.cos(t)*rx*wobble,cy+math.sin(t)*ry*wobble}
 end
 local mask={};local minx,maxx,miny,maxy=1e9,-1e9,1e9,-1e9
 for y=math.floor(cy-ry*1.4),math.ceil(cy+ry*1.4)do for x=math.floor(cx-rx*1.4),math.ceil(cx+rx*1.4)do
  local inside=false
  for i=1,#points do local a,b=points[i],points[i%#points+1]
   if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then
    if a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])>x then inside=not inside end end end
  if inside then mask[y*10000+x]=true;if x<minx then minx=x end;if x>maxx then maxx=x end;if y<miny then miny=y end;if y>maxy then maxy=y end end
 end end
 -- Khoang máy: thành vỏ dày ở rìa, lòng tối dần vào trong, 2 dầm ngang và conduit dọc.
 -- Bố cục cố định theo mask để đọc là kết cấu, không phải noise.
 local half=(miny+maxy)*.5;local span=math.max(1,maxy-miny)
 local stringerA=math.floor(half-span*.22);local stringerB=math.floor(half+span*.24)
 local conduit=math.floor((minx+maxx)*.5)
 for key in pairs(mask)do
  local x=key%10000;local y=math.floor(key/10000)
  if opaque(im,x,y)then
   local nx=(x-cx)/rx;local ny=(y-cy)/ry;local d=math.sqrt(nx*nx+ny*ny)
   -- Lòng khoang dùng neutral tối chung để mọi family đọc là "lỗ thủng", không phải recolor.
   local v=f.rampIdx[1]
   if d>=.82 then v=f.rampIdx[3]
   elseif d>=.62 then v=f.rampIdx[2]end
   if y==stringerA or y==stringerB then v=f.rampIdx[4]end
   if (y==stringerA-1 or y==stringerB-1)and d<.86 then v=f.rampIdx[1]end
   if math.abs(x-conduit)<=0 and ny>-.55 and ny<.55 then v=f.glowIdx[3]end
   if math.abs(x-conduit)==1 and ny>-.55 and ny<.55 then v=f.accentIdx[2]end
   if d>=.88 and d<.97 and (x+y)%7==0 then v=f.rampIdx[5]end
   im:putPixel(x,y,pal[v])
  end
 end
 -- Viền cắt: sáng ở mặt trên/trái, mid ở mép dưới; giữ 1px để silhouette còn sắc.
 for y=miny-1,maxy+1 do for x=minx-1,maxx+1 do
  if opaque(im,x,y)and not mask[y*10000+x]then
   local touches=false
   for dy=-1,1 do for dx=-1,1 do if (dx~=0 or dy~=0)and mask[(y+dy)*10000+x+dx]then touches=true end end end
   if touches then im:putPixel(x,y,pal[(not opaque(im,x,y-1)or not opaque(im,x-1,y))and f.rampIdx[6]or f.rampIdx[4]])end
  end
 end end
 -- Mảnh vỏ rách còn dính: chỉ vẽ khi tiếp giáp hull, không tạo chấm lơ lửng.
 for i=1,(deep and 4 or 2)do
  local ang=(i*2.4+seed)
  local bx=math.floor(cx+math.cos(ang)*rx)
  local by=math.floor(cy+math.sin(ang)*ry)
  local ox=math.cos(ang)>0 and 1 or -1;local oy=math.sin(ang)>0 and 1 or -1
  if opaque(im,bx,by)then
   for k=1,2 do
    local px=bx+ox*k;local py=by+oy*k
    if not opaque(im,px,py)and (opaque(im,px-ox,py)or opaque(im,px,py-oy))then
     im:putPixel(px,py,pal[f.rampIdx[k==1 and 5 or 3]])
    end
   end
  end
 end
 return cx,cy,rx,ry
end
local function crack(im,f,x,y,dx,dy,steps,seed)
 for i=0,steps do
  local px=math.floor(x+dx*i)
  local py=math.floor(y+dy*i+((i%4==0)and((seed+i)%2)or 0))
  if opaque(im,px,py)then
   im:putPixel(px,py,pal[f.rampIdx[1]])
   if i%3==1 and opaque(im,px+1,py)then im:putPixel(px+1,py,pal[f.rampIdx[2]])end
  end
 end
end
local sites={
 scout={{edge="left",v=.34,rx=.15,ry=.20},{edge="right",v=.62,rx=.17,ry=.24}},
 raider={{edge="left",v=.42,rx=.16,ry=.20},{edge="right",v=.58,rx=.18,ry=.26}},
 heavy={{edge="left",v=.30,rx=.13,ry=.18},{edge="right",v=.36,rx=.15,ry=.22}},
 bulwark={{edge="left",v=.26,rx=.13,ry=.20},{edge="right",v=.30,rx=.15,ry=.24}},
 sniper={{edge="left",v=.46,rx=.12,ry=.16},{edge="right",v=.52,rx=.14,ry=.20}},
 interceptor={{edge="left",v=.40,rx=.16,ry=.20},{edge="right",v=.56,rx=.18,ry=.26}},
 carrier={{edge="left",v=.46,rx=.11,ry=.16},{edge="right",v=.52,rx=.13,ry=.20}},
 supply={{edge="left",v=.38,rx=.14,ry=.18},{edge="right",v=.58,rx=.16,ry=.22}},
 vanguard={{edge="left",v=.36,rx=.10,ry=.12},{edge="right",v=.58,rx=.12,ry=.15}},
 warden={{edge="left",v=.36,rx=.10,ry=.12},{edge="right",v=.58,rx=.12,ry=.15}}
}
local function damage(im,f,key,stage)
 local result=Image(im.width,im.height);result:drawImage(im,Point(0,0))
 local minx,miny,maxx,maxy=bbox(im);local w,h=maxx-minx+1,maxy-miny+1
 local holes={}
 local count=stage==1 and 1 or 2
 for i=1,count do
  local spec=sites[key][math.min(i,#sites[key])]
  local rx=math.min(w*.20,math.max(3,w*spec.rx*(stage==1 and .78 or 1.30)))
  local ry=math.min(h*.24,math.max(3,h*spec.ry*(stage==1 and .78 or 1.30)))
  local cy=miny+h*spec.v
  local ex=edgeX(result,math.floor(cy-ry),math.ceil(cy+ry),spec.edge=="left")
  if ex then
   local cx=spec.edge=="left" and ex+rx*.55 or ex-rx*.55
   local a,b,c2,d=bite(result,f,cx,cy,rx,ry,stage*17+i,(stage==2))
   holes[#holes+1]={cx=a,cy=b,rx=c2,ry=d}
   local dx=spec.edge=="left" and 1 or -1
   crack(result,f,cx+rx*.9,cy-ry*.6,dx,1,math.floor(ry*.8),i)
   crack(result,f,cx-rx*.2,cy+ry*.4,dx,1,math.floor(ry*.5),i+9)
  end
 end
 if stage==2 then
  -- Sparks: cụm 2-4 pixel trắng-vàng ngay mép khoang, hướng ra ngoài.
  for i,hole in ipairs(holes)do
   for k=1,3 do
    local a=(i*2.1+k*1.7);local sx=math.floor(hole.cx+math.cos(a)*hole.rx*.9)
    local sy=math.floor(hole.cy+math.sin(a)*hole.ry*.9)
    if opaque(result,sx,sy)then result:putPixel(sx,sy,pal[c("fff6c8")])end
    if opaque(result,sx+1,sy)then result:putPixel(sx+1,sy,pal[c("ffd889")])end
   end
  end
  -- Khói: 2 cụm xám mờ nhỏ trên vỏ gần khoang, không rải khắp hull.
  for i,hole in ipairs(holes)do
   local sx=math.floor(hole.cx+hole.rx*.5);local sy=math.floor(hole.cy-hole.ry*.8)
   for y=sy-2,sy+2 do for x=sx-2,sx+2 do
    if opaque(result,x,y)and((x+y+i)%3~=0)then result:putPixel(x,y,pal[c("666f74")])end
   end end
  end
 end
 return result
end
local function exportFamily(key,familyName,ids)
 local f=families[familyName]
 for _,id in ipairs(ids)do
  if has(id)then
   local intact=retint(load(id),f)
   if id:find("damaged")or id:find("critical")then else
    save(intact,id)
    local damaged=damage(intact,f,key,1);save(damaged,id.."-damaged")
    local critical=damage(intact,f,key,2);save(critical,id.."-critical")
   end
  end
 end
end
-- Hull chính. Tool output cùng tên để prefab/damage binding không đổi.
exportFamily("scout","scout",{"scout"})
exportFamily("raider","raider",{"raider"})
exportFamily("heavy","heavy",{"heavy"})
exportFamily("bulwark","bulwark",{"bulwark"})
exportFamily("sniper","sniper",{"sniper"})
exportFamily("interceptor","interceptor",{"interceptor"})
exportFamily("carrier","carrier",{"carrier"})

-- Supply Drone: tàu vận tải cơ khí chở hộp quà/cargo crate ở trung tâm.
-- Crate amber với ribbon kem, đai hazard, kẹp kim loại và khóa năng lượng teal.
local function crate(im,cx,cy,w,h,glow)
 local A=families.supply.accentIdx
 local metal=c("353e3d");local metalLight=c("92a89b");local metalDark=c("1c2124")
 local x0,x1,y0,y1=math.floor(cx-w/2),math.floor(cx+w/2),math.floor(cy-h/2),math.floor(cy+h/2)
 -- Vỏ crate amber, chamfer 2px, nắp trên sáng.
 poly(im,{{x0+2,y0},{x1-2,y0},{x1,y0+2},{x1,y1-2},{x1-2,y1},{x0+2,y1},{x0,y1-2},{x0,y0+2}},A[1])
 poly(im,{{x0+3,y0+1},{x1-3,y0+1},{x1-1,y0+3},{x1-1,y1-3},{x1-3,y1-1},{x0+3,y1-1},{x0+1,y1-3},{x0+1,y0+3}},A[2])
 rect(im,x0+3,y0+2,w-7,2,A[3])
 poly(im,{{x0+1,y1-7},{x1-1,y1-7},{x1-3,y1-1},{x0+3,y1-1}},A[1])
 -- Ribbon dọc/ngang kem, viền tối 1px hai bên.
 rect(im,cx-2,y0+1,5,h-1,c("f7f1de"));line(im,cx-3,y0+2,cx-3,y1-2,metalDark);line(im,cx+3,y0+2,cx+3,y1-2,metalDark)
 rect(im,x0+2,cy-2,w-4,5,c("f7f1de"));line(im,x0+2,cy-3,x1-2,cy-3,metalDark);line(im,x0+2,cy+3,x1-2,cy+3,metalDark)
 -- Khóa năng lượng teal ở tâm ribbon.
 if glow then
  poly(im,{{cx,cy-4},{cx+4,cy},{cx,cy+4},{cx-4,cy}},c("41d2de"))
  poly(im,{{cx,cy-2},{cx+2,cy},{cx,cy+2},{cx-2,cy}},c("a0f1ec"))
 end
 -- Hai đai hazard ngang, không đè ribbon dọc.
 for _,band in ipairs{{y0+4,y0+7},{y1-8,y1-5}}do
  for x=x0+2,x1-2 do
   local v=((x+band[1])%6<3)and A[1]or metalDark
   for y=band[1],band[2] do put(im,x,y,v)end
  end
  line(im,x0+3,band[1],x1-3,band[1],metalLight)
 end
 -- Kẹp góc kim loại.
 for _,k in ipairs{{x0+1,y0+1,1,1},{x1-5,y0+1,-1,1},{x0+1,y1-5,1,-1},{x1-5,y1-5,-1,-1}}do
  local bx,by,dx,dy=k[1],k[2],k[3],k[4]
  rect(im,bx,by,5,2,metal);rect(im,bx,by,2,5,metal)
  put(im,bx+(dx>0 and 1 or 3),by+(dy>0 and 1 or 3),metalLight)
 end
 return x0,x1,y0,y1
end
-- Vết vỡ crate: lớp giấy gói bung ra, lộ hộp quà bên trong. Không đục thủng silhouette.
local function crateTear(im,cx,cy,rx,ry,seed,intense)
 local metalDark=c("1c2124")
 local points={};local count=10
 for i=1,count do
  local t=(i-1)/count*math.pi*2
  local wobble=1+jag(math.cos(t)*5,math.sin(t)*5,seed)*.26
  points[#points+1]={cx+math.cos(t)*rx*wobble,cy+math.sin(t)*ry*wobble}
 end
 local hole={}
 for y=math.floor(cy-ry*1.3),math.ceil(cy+ry*1.3)do for x=math.floor(cx-rx*1.3),math.ceil(cx+rx*1.3)do
  local inside=false
  for i=1,#points do local a,b=points[i],points[i%#points+1]
   if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then
    if a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])>x then inside=not inside end end end
  if inside and opaque(im,x,y)then hole[y*10000+x]=true end
 end end
 -- Hộp quà lộ ra: lòng hộp kín (không xuyên nền), ribbon kem, ánh lõi khi critical.
 for key in pairs(hole)do
  local x=key%10000;local y=math.floor(key/10000)
  local d=math.sqrt(((x-cx)/rx)^2+((y-cy)/ry)^2)
  local v=c("3b2836")
  if math.abs(x-cx)<=1 or math.abs(y-cy)<=1 then v=c("ffe0a0")end
  if intense and d<.72 and (x+y+seed)%4==0 then v=c("ff7d55")end
  if d>.86 then v=metalDark end
  im:putPixel(x,y,pal[v])
 end
 -- Nắp giấy bong: dải kem sáng men theo cạnh trên của vết rách.
 for key in pairs(hole)do
  local x=key%10000;local y=math.floor(key/10000)
  if opaque(im,x,y)then
   if not hole[(y-1)*10000+x]then im:putPixel(x,y,pal[c("fff6c8")])end
   if not hole[(y+1)*10000+x]and(y>cy)then im:putPixel(x,y,pal[c("c9751f")])end
  end
 end
 -- Vết nứt ngắn trên lớp giấy còn lại, chỉ 3-4px để không thành noise.
 for i=1,4 do
  local px=math.floor(cx+rx*(.9+i*.06));local py=math.floor(cy-ry*.5+i*2)
  if opaque(im,px,py)then im:putPixel(px,py,pal[metalDark])end
 end
end
local function supplyShip()
 local im=image(112,80)
 local R=families.supply.rampIdx
 local A=families.supply.accentIdx
 local metal=c("353e3d");local metalLight=c("92a89b");local metalDark=c("1c2124")
 -- Hai cụm động cơ hai bên, exhaust hướng lên trên (đuôi tàu).
 for _,side in ipairs{{10,1},{80,-1}}do
  local x=side[1]
  poly(im,{{x+3,20},{x+19,20},{x+22,26},{x+22,50},{x+18,57},{x+4,57},{x,50},{x,26}},R[2])
  poly(im,{{x+5,22},{x+17,22},{x+19,27},{x+19,49},{x+16,54},{x+6,54},{x+3,49},{x+3,27}},R[4])
  poly(im,{{x+5,23},{x+17,23},{x+18,27},{x+5,27}},R[5])
  rect(im,x+5,30,12,2,R[3]);rect(im,x+5,44,12,2,R[3])
  rect(im,x+6,34,10,8,metal);rect(im,x+7,35,8,6,R[2])
  -- Exhaust nozzle + plume.
  rect(im,x+4,13,14,8,metalDark);rect(im,x+5,14,12,5,A[1]);rect(im,x+7,15,8,3,A[2]);rect(im,x+9,16,4,1,A[4])
  -- Cánh ổn định ngoài.
  poly(im,{{x+20,32},{x+30,26},{x+30,42},{x+20,46}},R[3])
  poly(im,{{x+21,34},{x+28,29},{x+28,40},{x+21,44}},R[4])
  line(im,x+20,32,x+30,26,R[6])
 end
 -- Khung gantry giữ crate: thép sáng để tách khỏi viền navy, có lỗ giảm trọng lượng.
 rect(im,30,18,5,48,metal);rect(im,77,18,5,48,metal)
 rect(im,31,19,3,46,metalLight);rect(im,78,19,3,46,metalLight)
 for y=26,58,8 do rect(im,31,y,3,3,metalDark);rect(im,78,y,3,3,metalDark)end
 -- Thanh giằng ngang trên/dưới crate.
 rect(im,36,14,40,5,metal);rect(im,36,61,40,5,metal)
 rect(im,37,15,38,2,metalLight);rect(im,37,62,38,2,metal)
 -- Thanh chéo giằng góc.
 line(im,30,18,36,14,metalLight);line(im,82,18,76,14,metalLight)
 line(im,30,66,36,61,metalLight);line(im,82,66,76,61,metalLight)
 -- Crate trung tâm.
 local x0,x1,y0,y1=crate(im,56,38,34,42,true)
 -- Mũi tàu phía dưới: cụm cảm biến + đèn hạ cánh.
 poly(im,{{48,64},{64,64},{62,76},{58,79},{54,79},{50,76}},R[2])
 poly(im,{{50,66},{62,66},{60,75},{57,77},{55,77},{52,75}},R[3])
 rect(im,54,68,4,4,metalDark);put(im,55,69,A[4]);put(im,56,69,A[3])
 -- Đèn định vị hai bên.
 put(im,8,22,c("ff6a4a"));put(im,103,22,c("ff6a4a"))
 return im
end
local supply=supplyShip();save(supply,"supply-drone")
local supplyDamaged=Image(supply);crateTear(supplyDamaged,70,26,13,11,3,false);crateTear(supplyDamaged,44,54,7,6,7,false)
save(supplyDamaged,"supply-drone-damaged")
local supplyCritical=Image(supply);crateTear(supplyCritical,70,26,16,14,3,true);crateTear(supplyCritical,43,55,10,9,7,true)
-- Critical: mất một nắp kẹp, crate nghiêng.
rect(supplyCritical,36,58,12,6,0)
save(supplyCritical,"supply-drone-critical")
-- Gift burst: panel crate bung ra và hộp quà xuất hiện.
do
 local frames={}
 for f=0,3 do
  local a=image(64,64)
  local spread=4+f*6
  for i=1,6 do
   local ang=i*math.pi/3+.3
   local px=32+math.cos(ang)*spread;local py=32+math.sin(ang)*spread
   if f<3 then
    rect(a,px-2,py-2,5,4,c("8a4a12"));rect(a,px-2,py-2,5,1,c("f2a333"))
   end
  end
  if f>=1 then
   local s=math.min(1,f/2);local w=math.floor(10+12*s);local h=math.floor(9+11*s)
   rect(a,32-w/2,32-h/2,w,h,c("f2a333"));rect(a,32-w/2,32-h/2,w,2,c("ffd889"))
   rect(a,31,32-h/2,2,h,c("f7f1de"));rect(a,32-w/2,31,w,2,c("f7f1de"))
   if f>=2 then rect(a,28,28,8,3,c("41d2de"))end
  end
  if f<3 then
   for i=1,8 do local ang=i*math.pi/4+f;local r=6+f*7
    local px=32+math.cos(ang)*r;local py=32+math.sin(ang)*r
    put(a,px,py,c("ffe98a"));put(a,px+1,py,c("ff6a4a"))end
  end
  frames[#frames+1]=a
 end
 saveSheet("supply-burst",64,64,frames,.07)
end

-- Warden drone reuse Scout silhouette nhưng theo palette Warden.
do
 local src=load("scout");local small=Image(src);small:resize(48,38)
 local im=image(48,48);im:drawImage(small,Point(0,5))
 local f=families.warden;local tinted=retint(im,f);save(tinted,"warden-drone")
 save(damage(tinted,f,"sniper",1),"warden-drone-damaged")
 save(damage(tinted,f,"sniper",2),"warden-drone-critical")
end
-- Boss modules: retint family riêng, hull/module nhận damage state.
exportFamily("vanguard","vanguard",{"vanguard-hull","vanguard-wing","vanguard-engine","vanguard-reactor","vanguard-missile-pod","vanguard-beam-pod","vanguard-truss"})
exportFamily("warden","warden",{"warden-hull","warden-wing","warden-core","warden-beam-node","warden-drone-node","warden-plasma-node","warden-truss"})
-- Boss damage state dùng bite nhỏ hơn để không phá module silhouette.
do
 local f=families.vanguard;local hull=retint(load("vanguard-hull"),f)
 save(damage(hull,f,"vanguard",1),"vanguard-damaged");save(damage(hull,f,"vanguard",2),"vanguard-critical")
 local w=families.warden;local wh=retint(load("warden-hull"),w)
 save(damage(wh,w,"warden",1),"warden-damaged");save(damage(wh,w,"warden",2),"warden-critical")
end
print("PASS fleet families and damage states")

-- Projectile family: mỗi branch có charge/muzzle/hit/trail riêng, hostile coral/lemon
-- phân biệt với friendly cyan và plasma tím. Magenta giữ riêng cho special danger.
local function bolt(id,w,h,outline,body,core,spark)
 local frames={}
 for f=0,3 do
  local im=image(w,h)
  local hw=math.floor(w/2)
  -- Viền tối 1px + thân sáng + lõi trắng: đọc rõ trên nền navy ở gameplay scale.
  poly(im,{{hw,1},{w-1,math.floor(h*.30)},{w-2,math.floor(h*.66)},{hw,h-1},{1,math.floor(h*.66)},{0,math.floor(h*.30)}},c(outline))
  poly(im,{{hw,2},{w-2,math.floor(h*.32)},{w-3,math.floor(h*.62)},{hw,h-2},{2,math.floor(h*.62)},{1,math.floor(h*.32)}},c(body))
  poly(im,{{hw,4},{w-3,math.floor(h*.34)},{w-4,math.floor(h*.54)},{hw,h-4},{3,math.floor(h*.54)},{2,math.floor(h*.34)}},c(core))
  rect(im,hw,math.floor(h*.32),1,math.floor(h*.26),c(spark))
  -- Vệt đuôi ngắn mở dần theo frame.
  if f>0 then
   for i=0,(f==3 and 2 or 1)do
    put(im,hw-1-i,h-1,c(body));put(im,hw+i,h-1,c(body))
   end
  end
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.06)
 frames[1]:saveAs(out.."/"..id..".png")
end
bolt("pulse-bolt",12,32,"0b3948","1b9bb2","a0f1ec","e5f4f2")
bolt("plasma-orb",16,20,"271b32","7a52e0","d4bbff","e5f4f2")
bolt("hostile-coral",14,22,"5c1714","e5482f","ffb69c","fff3b0")
bolt("hostile-lemon",14,22,"3a3010","e8d24a","fffbc0","ffffff")
-- Special danger: magenta, silhouette lớn hơn + gai để không lẫn coral.
do
 local frames={}
 for f=0,3 do
  local im=image(20,24);local cx=10
  -- 4 frame khác nhau: thân phình nhẹ và gai mọc dần, không lặp lại 2 frame.
  local grow=f
  poly(im,{{cx,1},{18+grow,7},{18+grow,17},{cx,23},{2-grow,17},{2-grow,7}},c("271b32"))
  poly(im,{{cx,3},{16+grow,8},{16+grow,16},{cx,21},{4-grow,16},{4-grow,8}},c("ff2fd0"))
  poly(im,{{cx,6},{13+grow,10},{13+grow,14},{cx,18},{7-grow,14},{7-grow,10}},c("ff9ee8"))
  rect(im,cx-1,9,2,6,c("ffffff"))
  put(im,cx,0,c("ff9ee8"))
  for i=0,grow do put(im,cx-3-i,13,c("ffd0f0"));put(im,cx+3+i,13,c("ffd0f0"))end
  frames[#frames+1]=im
 end
 saveSheet("hostile-special",20,24,frames,.07)
 frames[1]:saveAs(out.."/hostile-special.png")
end
-- Charge cue theo branch: vòng co vào lõi, màu riêng.
local function charge(id,w,h,ring,core,accent)
 local frames={}
 for f=0,3 do
  local im=image(w,h);local cx,cy=w/2,h/2
  local r=math.max(2,(w/2-2)-(f*2))
  for k=0,7 do
   local t=k*math.pi/4+f*.2
   local x=cx+math.cos(t)*r;local y=cy+math.sin(t)*r
   put(im,x,y,c(ring));put(im,x+math.cos(t),y+math.sin(t),c(accent))
  end
  for k=0,5 do
   local t=k*math.pi/3-f*.3
   local x=cx+math.cos(t)*(r*.55);local y=cy+math.sin(t)*(r*.55)
   put(im,x,y,c(core))
  end
  disc(im,cx,cy,1+f*.5,1+f*.5,c(accent))
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.06)
 frames[1]:saveAs(out.."/"..id..".png")
end
charge("charge-pulse",24,24,"41d2de","a0f1ec","e5f4f2")
charge("charge-plasma",26,26,"7a52e0","d4bbff","e5f4f2")
charge("charge-missile",24,24,"ff7d55","ffd889","fff6c8")
charge("charge-beam",28,28,"ff2fd0","ff9ee8","ffffff")
-- Hostile thường: vòng coral/lemon, không dùng magenta của special.
charge("charge-hostile",26,26,"ff7d55","ffb69c","fff3b0")
charge("charge-hostile-lemon",24,24,"e8d24a","fffbc0","ffffff")
-- Muzzle theo branch: 4 frame, silhouette khác nhau rõ.
local function muzzle(id,w,h,main,hot,shape)
 local frames={}
 for f=0,3 do
  local im=image(w,h);local cx,cy=w/2,h/2
  if shape=="needle" then
   local len=math.floor(w*.36-f*2)
   poly(im,{{cx,cy-len},{cx+3,cy-len*.55},{cx+2,cy},{cx-2,cy},{cx-3,cy-len*.55}},c(main))
   poly(im,{{cx,cy-len+2},{cx+2,cy-len*.55},{cx+1,cy-1},{cx-1,cy-1},{cx-2,cy-len*.55}},c(hot))
   disc(im,cx,cy,2,2,c("ffffff"))
   for s=-1,1,2 do line(im,cx+s*3,cy+2,cx+s*(5+f*2),cy+5+f*2,c(main))end
  elseif shape=="orb" then
   local r=math.floor(w*.18+f*1.6)
   disc(im,cx,cy,r,r,c(main));disc(im,cx,cy,r*.55,r*.55,c(hot))
   for k=0,5 do local t=k*math.pi/3+f;line(im,cx+math.cos(t)*r,cy+math.sin(t)*r,cx+math.cos(t)*(r+3+f),cy+math.sin(t)*(r+3+f),c(hot))end
  elseif shape=="rocket" then
   rect(im,cx-3,cy-7,6,11,c(main));rect(im,cx-2,cy-6,4,9,c(hot))
   poly(im,{{cx-4,cy+4},{cx-8-f*2,cy+10+f*2},{cx,cy+7},{cx+8+f*2,cy+10+f*2},{cx+4,cy+4}},c(main))
   poly(im,{{cx-2,cy+4},{cx-5-f,cy+7+f},{cx,cy+6},{cx+5+f,cy+7+f},{cx+2,cy+4}},c("fff6c8"))
  else -- beam
   local len=math.floor(h*.5-f*3)
   for x=-2,2 do rect(im,cx+x,cy-len,1,len*2,x==0 and c("ffffff")or c(hot))end
   for s=-1,1,2 do line(im,cx+s*4,cy-2,cx+s*(6+f*3),cy-6-f*2,c(main))end
   disc(im,cx,cy,3,3,c(hot))
  end
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.05)
 frames[1]:saveAs(out.."/"..id..".png")
end
muzzle("muzzle-pulse",32,32,"1b9bb2","a0f1ec","needle")
muzzle("muzzle-plasma",32,32,"7a52e0","d4bbff","orb")
muzzle("muzzle-missile",32,32,"e8aa58","fff6c8","rocket")
muzzle("muzzle-beam",32,32,"ff2fd0","ff9ee8","beam")
-- Hit: mảnh văng theo hướng, màu khớp nguồn đạn.
local function hit(id,w,h,main,hot,radial)
 local frames={}
 for f=0,3 do
  local im=image(w,h);local cx,cy=w/2,h/2
  for k=0,7 do
   local t=k*math.pi/4+(radial and .4 or 0)
   local r=3+f*4
   local x=cx+math.cos(t)*r;local y=cy+math.sin(t)*r
   put(im,x,y,c(hot))
   if f<3 then put(im,x+math.cos(t),y+math.sin(t),c(main))end
  end
  if f<2 then disc(im,cx,cy,3-f,3-f,c(hot));disc(im,cx,cy,1,1,c("ffffff"))end
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.05)
 frames[1]:saveAs(out.."/"..id..".png")
end
hit("hit-pulse",24,24,"1b9bb2","a0f1ec",false)
hit("hit-plasma",28,28,"7a52e0","d4bbff",true)
hit("hit-missile",32,32,"e8aa58","ff6a4a",true)
hit("hit-beam",28,28,"ff2fd0","ff9ee8",false)
-- Trail dot theo branch (cho trail/particle rời, không đổi TrailRenderer).
local function dot(id,w,h,main,hot)
 local im=image(w,h);local cx,cy=w/2,h/2
 disc(im,cx,cy,w*.5,w*.5,c(main));disc(im,cx,cy,w*.3,h*.3,c(hot))
 save(im,id)
end
dot("trail-dot-pulse",8,8,"1b9bb2","a0f1ec")
dot("trail-dot-plasma",10,10,"7a52e0","d4bbff")
dot("trail-dot-missile",10,10,"ff6a4a","fff6c8")
dot("trail-dot-beam",10,10,"ff2fd0","ff9ee8")
-- Telegraph bracket coral/lemon cho normal hostile, magenta cho special.
local function brackets(id,w,h,color,wide)
 local frames={}
 for f=0,3 do
  local im=image(w,h);local gap=2+f*(wide and 2 or 1)
  for _,q in ipairs{{gap,gap,1,1},{w-1-gap,gap,-1,1},{gap,h-1-gap,1,-1},{w-1-gap,h-1-gap,-1,-1}}do
   for d=0,5 do put(im,q[1]+d*q[3],q[2],c(color))end
   for d=0,5 do put(im,q[1],q[2]+d*q[4],c(color))end
   put(im,q[1]+q[3],q[2]+q[4],c("ffffff"))
  end
  if wide then for i=0,w-1 do put(im,i,f==0 and 0 or h-1,c(color))end end
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.06)
 frames[1]:saveAs(out.."/"..id..".png")
end
brackets("telegraph-coral",28,28,"ff7d55",false)
brackets("telegraph-lemon",28,28,"fffbc0",false)
brackets("telegraph-special",30,30,"ff2fd0",true)
-- Beam charge lane: cột cảnh báo dọc, đậm dần rồi mở.
local function lane(id,w,h,danger,hot)
 local frames={}
 for f=0,3 do
  local im=image(w,h);local cx=w/2
  local half=math.floor(1+f*2)
  for y=0,h-1 do
   for x=cx-half,cx+half do
    local edge=(x==cx-half or x==cx+half)
    put(im,x,y,c(edge and hot or danger))
   end
  end
  for y=2,h-2,6 do rect(im,cx-1,y,2,3,c("ffffff"))end
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.07)
 frames[1]:saveAs(out.."/"..id..".png")
end
lane("beam-warning",16,64,"ff2fd0","ffd0f0")

-- Impact theo loại trúng: shield block, hull và module có silhouette/màu khác nhau.
local function impactRing(id,w,h,r0,r1,inner,outer,spark,count)
 local frames={}
 for f=0,3 do
  local im=image(w,h);local cx,cy=w/2,h/2
  local r=r0+(r1-r0)*(f/3)
  for k=0,(count or 12)-1 do
   local t=k*math.pi*2/(count or 12)+.2
   put(im,cx+math.cos(t)*r,cy+math.sin(t)*r,c(outer))
   put(im,cx+math.cos(t)*(r-1),cy+math.sin(t)*(r-1),c(inner))
  end
  if f<2 then
   for k=0,5 do local t=k*math.pi/3+.4;line(im,cx+math.cos(t)*3,cy+math.sin(t)*3,cx+math.cos(t)*(6+f*4),cy+math.sin(t)*(6+f*4),c(spark))end
  end
  frames[#frames+1]=im
 end
 saveSheet(id,w,h,frames,.055)
 frames[1]:saveAs(out.."/"..id..".png")
end
-- Shield block: vòng teal + lục giác, đọc là "bị chặn", không phải hull hit.
do
 local frames={}
 for f=0,3 do
  local im=image(64,64);local cx,cy=32,32
  local r=14+f*4
  for k=0,5 do
   local t=k*math.pi/3+math.pi/6
   local x0,y0=cx+math.cos(t)*r,cy+math.sin(t)*r
   local t2=t+math.pi/3
   local x1,y1=cx+math.cos(t2)*r,cy+math.sin(t2)*r
   line(im,x0,y0,x1,y1,c("41d2de"))
   if f<3 then line(im,x0+math.cos(t),y0+math.sin(t),x1+math.cos(t2),y1+math.sin(t2),c("a0f1ec"))end
  end
  disc(im,cx,cy,5-f,5-f,c("a0f1ec"),.6)
  if f<2 then
   for k=0,5 do local t=k*math.pi/3;line(im,cx+math.cos(t)*6,cy+math.sin(t)*6,cx+math.cos(t)*16,cy+math.sin(t)*16,c("e5f4f2"))end
  end
  frames[#frames+1]=im
 end
 saveSheet("impact-shield",64,64,frames,.06)
 frames[1]:saveAs(out.."/impact-shield.png")
end
-- Hull hit: sparks vàng-cam + mảnh tối, không có vòng.
do
 local frames={}
 for f=0,3 do
  local im=image(40,40);local cx,cy=20,20
  for k=0,9 do
   local t=k*math.pi/5+.15;local r=2+f*5
   local x=cx+math.cos(t)*r;local y=cy+math.sin(t)*r
   put(im,x,y,c("fff3b0"));put(im,x+math.cos(t),y+math.sin(t),c("ff7d55"))
   if f<2 then put(im,x-math.cos(t),y-math.sin(t),c("ffffff"))end
  end
  if f<2 then disc(im,cx,cy,3-f,3-f,c("fff6c8"))end
  if f>1 then for k=0,3 do local t=k*math.pi/2+.7;put(im,cx+math.cos(t)*(4+f*2),cy+math.sin(t)*(4+f*2),c("3a4348"))end end
  frames[#frames+1]=im
 end
 saveSheet("impact-hull",40,40,frames,.055)
 frames[1]:saveAs(out.."/impact-hull.png")
end
-- Module hit: vòng chữ nhật vuông góc + tia ngắn, đọc khác hull/shield.
do
 local frames={}
 for f=0,3 do
  local im=image(48,48);local cx,cy=24,24
  local r=8+f*4
  local function seg(x0,y0,x1,y1)for t=0,1,.08 do put(im,x0+(x1-x0)*t,y0+(y1-y0)*t,c("ffd889"))end end
  seg(cx-r,cy-r,cx-r+5,cy-r);seg(cx+r-5,cy-r,cx+r,cy-r)
  seg(cx-r,cy+r,cx-r+5,cy+r);seg(cx+r-5,cy+r,cx+r,cy+r)
  seg(cx-r,cy-r,cx-r,cy-r+5);seg(cx+r,cy-r,cx+r,cy-r+5)
  seg(cx-r,cy+r-5,cx-r,cy+r);seg(cx+r,cy+r-5,cx+r,cy+r)
  if f<2 then
   for k=0,3 do local sx=cx+((k%2==0)and -1 or 1)*(6+f*6);local sy=cy+((k<2)and -1 or 1)*(6+f*6)
    line(im,sx,sy,sx+((k%2==0)and -1 or 1)*4,sy,c("fff6c8"))end
  end
  frames[#frames+1]=im
 end
 saveSheet("impact-module",48,48,frames,.055)
 frames[1]:saveAs(out.."/impact-module.png")
end
-- Debris độc lập cho hull breakup: mảnh vỏ nhiều cỡ, xoay theo frame.
do
 local frames={}
 for f=0,3 do
  local im=image(48,48);local cx,cy=24,24
  for i=1,7 do
   local t=i*math.pi*2/7+.5
   local r=3+f*5
   local x=cx+math.cos(t)*r;local y=cy+math.sin(t)*r
   local s=math.max(1,3-f%2)
   local v=(i%3==0)and c("92a89b")or((i%2==0)and c("56645f")or c("353e3d"))
   poly(im,{{x-s,y-s},{x+s,y-s+1},{x+s-1,y+s},{x-s+1,y+s}},v)
   if i%3==0 then put(im,x,y,c("ff7d55"))end
  end
  frames[#frames+1]=im
 end
 saveSheet("debris-hull",48,48,frames,.075)
 frames[1]:saveAs(out.."/debris-hull.png")
end
-- Heavy/boss fragment lớn hơn, có lửa và khói cuộn.
do
 local frames={}
 for f=0,3 do
  local im=image(64,64);local cx,cy=32,32
  for i=1,9 do
   local t=i*math.pi*2/9+.3
   local r=4+f*7
   local x=cx+math.cos(t)*r;local y=cy+math.sin(t)*r
   local s=math.max(1,4-f)
   poly(im,{{x-s,y-s},{x+s,y-s+2},{x+s-1,y+s},{x-s+1,y+s}},c("3b2836"))
   poly(im,{{x-s+1,y-s+1},{x+s-1,y-s+1},{x+s-2,y+1},{x-s+2,y+1}},c(i%2==0 and "745047" or "525a22"))
   if i%3==0 then disc(im,x,y,2,2,c("ff7d55"))end
  end
  if f<3 then disc(im,cx,cy,6-f*2,6-f*2,c("fff3b0"))end
  if f>1 then
   for i=1,4 do local t=i*math.pi/2+f;local r=8+f*3
    disc(im,cx+math.cos(t)*r,cy+math.sin(t)*r,4,3,c("6b7578"))end
  end
  frames[#frames+1]=im
 end
 saveSheet("debris-heavy",64,64,frames,.08)
 frames[1]:saveAs(out.."/debris-heavy.png")
end
-- Asteroid damage: mất khối, nứt sâu, bong đá; resource giữ crystal identity.
do
 local function rockDamage(src,id,seed,deep)
  local im=Image(src.width,src.height);im:drawImage(src,Point(0,0))
  local cx,cy=src.width*.42,src.height*.46
  local rx,ry=src.width*(deep and .20 or .15),src.height*(deep and .18 or .13)
  local points={};local count=8
  for i=1,count do
   local t=(i-1)/count*math.pi*2
   local wobble=1+jag(math.cos(t)*4,math.sin(t)*4,seed)*(deep and .34 or .24)
   points[#points+1]={cx+math.cos(t)*rx*wobble,cy+math.sin(t)*ry*wobble}
  end
  for y=math.floor(cy-ry*1.5),math.ceil(cy+ry*1.5)do for x=math.floor(cx-rx*1.5),math.ceil(cx+rx*1.5)do
   local inside=false
   for i=1,#points do local a,b=points[i],points[i%#points+1]
    if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then
     if a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])>x then inside=not inside end end end
   if inside and opaque(im,x,y)then
    local d=math.sqrt(((x-cx)/rx)^2+((y-cy)/ry)^2)
    if d<.5 then clear(im,x,y)
    else im:putPixel(x,y,pal[c(d>.82 and "1c2124"or(d>.65 and "3b2836"or "6b7578"))])end
   end
  end end
  -- Mép vỡ sáng đá + vết nứt sâu.
  for y=math.floor(cy-ry*1.6),math.ceil(cy+ry*1.6)do for x=math.floor(cx-rx*1.6),math.ceil(cx+rx*1.6)do
   if opaque(im,x,y)and(not opaque(im,x+1,y)or not opaque(im,x,y+1))and((x+y)%3==0)then im:putPixel(x,y,pal[c("a6bfcb")])end
  end end
  for i=0,math.floor(ry*1.4)do
   local px=math.floor(cx+rx*.6+i);local py=math.floor(cy-ry*.8+i*.8)
   if opaque(im,px,py)then im:putPixel(px,py,pal[c("1c2124")])end
   if opaque(im,px+1,py)and i%3==0 then im:putPixel(px+1,py,pal[c("6e899b")])end
  end
  -- Đá bong: mảnh nhỏ rời gần vết vỡ.
  for i=1,(deep and 5 or 3)do
   local t=i*1.7+seed;local r=rx*(1.1+i*.18)
   local px=math.floor(cx+math.cos(t)*r);local py=math.floor(cy+math.sin(t)*r)
   if not opaque(im,px,py)then
    put(im,px,py,c(i%2==0 and "745047"or "3b2836"))
    if i%2==1 then put(im,px+1,py,c("6e899b"))end
   end
  end
  save(im,id)
 end
 local anchor=load("asteroid-anchor")
 for _,spec in ipairs{{"small",48},{"medium",64},{"large",96},{"huge",128}}do
  local base=Image(anchor);base:resize(spec[2],spec[2])
  rockDamage(base,"asteroid-"..spec[1].."-damaged",3,false)
  rockDamage(base,"asteroid-"..spec[1].."-critical",7,true)
 end
 local resource=load("resource-asteroid")
 rockDamage(resource,"resource-asteroid-damaged",5,false)
 -- Resource critical: vết vỡ lớn, lộ cụm tinh thể amber nhiều mặt thay vì lỗ trống.
 local critical=image(resource.width,resource.height);critical:drawImage(resource,Point(0,0))
 local cx,cy=resource.width*.42,resource.height*.46
 local points={};local count=9
 for i=1,count do
  local t=(i-1)/count*math.pi*2
  local wobble=1+jag(math.cos(t)*4,math.sin(t)*4,9)*.32
  points[#points+1]={cx+math.cos(t)*resource.width*.22*wobble,cy+math.sin(t)*resource.height*.20*wobble}
 end
 for y=math.floor(cy-resource.height*.30),math.ceil(cy+resource.height*.30)do for x=math.floor(cx-resource.width*.32),math.ceil(cx+resource.width*.32)do
  local inside=false
  for i=1,#points do local a,b=points[i],points[i%#points+1]
   if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then
    if a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])>x then inside=not inside end end end
  if inside and opaque(critical,x,y)then critical:putPixel(x,y,pal[c("1c2124")])end
 end end
 -- Cụm 3 tinh thể: cột chính + 2 cột phụ, mỗi mặt một value band.
 local function crystal(ox,oy,w,h,v1,v2,v3)
  local hw=math.floor(w/2)
  poly(critical,{{ox,oy-h},{ox+hw,oy-math.floor(h*.35)},{ox+hw,oy+math.floor(h*.4)},{ox,oy+h},{ox-hw,oy+math.floor(h*.4)},{ox-hw,oy-math.floor(h*.35)}},c("8a4a12"))
  poly(critical,{{ox,oy-h+1},{ox+hw-1,oy-math.floor(h*.34)},{ox+hw-1,oy+math.floor(h*.38)},{ox,oy+h-1},{ox-hw+1,oy+math.floor(h*.38)},{ox-hw+1,oy-math.floor(h*.34)}},c(v1))
  poly(critical,{{ox,oy-h+1},{ox,oy+h-1},{ox-hw+1,oy+math.floor(h*.38)},{ox-hw+1,oy-math.floor(h*.34)}},c(v2))
  poly(critical,{{ox,oy-h+1},{ox+hw-1,oy-math.floor(h*.34)},{ox+math.floor(hw*.3),oy-math.floor(h*.1)},{ox+2,oy+math.floor(h*.1)}},c(v3))
  -- Cạnh sáng trên đỉnh và mặt trái.
  line(critical,ox,oy-h+1,ox+hw-1,oy-math.floor(h*.34),c("fffbc0"))
  line(critical,ox,oy-h+1,ox-hw+1,oy-math.floor(h*.34),c("fffbc0"))
 end
 crystal(math.floor(cx),math.floor(cy),14,26,"f2a333","c9751f","ffd889")
 crystal(math.floor(cx-9),math.floor(cy+6),8,16,"e8aa58","ad704b","f7c94a")
 crystal(math.floor(cx+9),math.floor(cy+4),7,14,"f7c94a","c9751f","fffbc0")
 save(critical,"resource-asteroid-critical")
end
print("PASS impacts, debris and asteroid damage")

-- Boss/heavy staged explosion: cụm lửa đỏ-cam có lõi trắng, mảnh vỏ, khói cuộn.
do
 local frames={}
 for f=0,3 do
  local im=image(64,64);local cx,cy=32,32
  for i=1,8 do
   local t=i*math.pi/4+.25
   local r=2+f*6
   local x=cx+math.cos(t)*r;local y=cy+math.sin(t)*r
   local s=math.max(2,6-f)
   disc(im,x,y,s,s,c(f<2 and "ff7d55"or "ff6a4a"))
   disc(im,x,y,s*.55,s*.55,c(f<2 and "fff3b0"or "ffd889"))
  end
  if f<3 then disc(im,cx,cy,7-f*2,7-f*2,c("ffffff"))end
  if f>1 then
   for i=1,5 do
    local t=i*math.pi*2/5+1.1;local r=6+f*4
    disc(im,cx+math.cos(t)*r,cy+math.sin(t)*r,4,3,c("6b7578"))
   end
  end
  frames[#frames+1]=im
 end
 saveSheet("boss-blast",64,64,frames,.07)
 frames[1]:saveAs(out.."/boss-blast.png")
end
-- Module collapse: tia phản hồi ngắn, dùng cho shield node/module bị phá.
do
 local frames={}
 for f=0,3 do
  local im=image(48,48);local cx,cy=24,24
  local r=4+f*6
  for k=0,11 do
   local t=k*math.pi/6+f*.15
   local inner=r*.45;local outer=r
   line(im,cx+math.cos(t)*inner,cy+math.sin(t)*inner,cx+math.cos(t)*outer,cy+math.sin(t)*outer,c(k%2==0 and "ffd889"or "ff7d55"))
  end
  if f<2 then disc(im,cx,cy,4-f,4-f,c("ffffff"))end
  frames[#frames+1]=im
 end
 saveSheet("module-collapse",48,48,frames,.06)
 frames[1]:saveAs(out.."/module-collapse.png")
end
print("PASS boss destruction effects")
