local root=assert(app.params.root);local out=assert(app.params.out)
local hex={"080f20","111d32","1c2e46","2d4660","46617a","6e899b","a6bfcb","e5f4f2","0b3948","126578","1b9bb2","41d2de","a0f1ec","271b32","50273d","87354c","bb4057","ed6470","ffb69c","3b2836","745047","ad704b","e8aa58","ffe0a0","282846","505073","8484ac","c0b8d1","464025","7e7745","bab773","f1edb3"}
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

-- Final sources: run only this recipe, not historical M8 migrations.
local function normalizeFinal(file,id)
 local raw=Image{fromFile=root.."/sources/"..file.."-raw.png"}; raw:resize(64,80)
 local im=image(64,80)
 for y=0,79 do for x=0,63 do local v=raw:getPixel(x,y)
 if app.pixelColor.rgbaA(v)>=128 then
 local r,g,b=app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v);local best,d=1,1e12
 for i,q in ipairs(pal)do local dd=(r-app.pixelColor.rgbaR(q))^2+(g-app.pixelColor.rgbaG(q))^2+(b-app.pixelColor.rgbaB(q))^2;if dd<d then best=i;d=dd end end
 put(im,x,y,best)end end end
 save(im,id)
 local amber=Image(im)
 for y=25,69 do for x=0,63 do
 local v=im:getPixel(x,y);local outer=math.abs(x-31.5)>7
 if outer and (v==pal[7]or v==pal[8]or v==pal[6])then amber:putPixel(x,y,v==pal[8]and pal[24]or v==pal[7]and pal[23]or pal[22])end end end
 save(amber,id.."-amber")
end
normalizeFinal("player-spear-final","player")
normalizeFinal("player-raptor-final","player-swept")
local function animation(id,w,h,frames,seconds)
 local s=Sprite(w,h,ColorMode.RGB);local sheet=image(w*#frames,h)
 for f,a in ipairs(frames)do if f>1 then s:newFrame()end;s:newCel(s.layers[1],f,a,Point(0,0));s.frames[f].duration=seconds;copy(sheet,a,(f-1)*w,0)end
 s:saveAs(root.."/cleaned/"..id..".aseprite");s:close();frames[1]:saveAs(out.."/"..id..".png");sheet:saveAs(out.."/"..id.."-sheet.png")
end
do
 local frames={}
 for f=0,3 do local a=image(32,40)
 poly(a,{{8,2},{24,2},{23,12},{20,23+f*2},{16,36-f*2},{12,23+f*2},{9,12}},9)
 poly(a,{{10,2},{22,2},{21,12},{18,25+f*2},{16,31+f},{14,25+f*2},{11,12}},11)
 poly(a,{{12,2},{20,2},{19,12},{16,23+f},{13,12}},13)
 rect(a,14,2,4,11+f,8);line(a,16,25+f,16,31+f,12)
 if f%2==0 then rect(a,15,35,2,3,10)end
 frames[#frames+1]=a end
 animation("engine-loop",32,40,frames,.075)
end
for _,id in ipairs{"trail-standard","trail-ion","trail-rainbow"}do
 local frames={}
 for f=0,3 do local a=image(16,64)
 for y=0,61 do
 local width=math.max(1,math.floor((1-y/64)*5))
 for x=8-width,7+width do
 local c=id=="trail-standard"and(y<20 and 11 or 10)or id=="trail-ion"and(y<35 and 12 or 10)or ({18,23,31,12,27,28})[1+math.floor((x-(8-width))*6/(width*2))]
 if y<45 or (x+y+f)%3~=0 then put(a,x,y,c)end
 end
 if id~="trail-rainbow"and y<28-f*2 then rect(a,7,y,2,1,id=="trail-ion"and 8 or 13)end
 end
 for k=0,2 do local y=(f*9+k*19)%62;rect(a,7,y,2,3,id=="trail-rainbow"and 24 or 13)end
 frames[#frames+1]=a end
 animation(id,16,64,frames,.08)
end


-- Sửa nose và nacelle theo trục giữa; không để chân lửa trôi khỏi cửa xả.
do
 local a=load("player")
 rect(a,23,0,19,31,0)
 poly(a,{{31,1},{32,1},{35,14},{37,36},{26,36},{28,14}},1)
 poly(a,{{31,3},{32,3},{34,15},{35,35},{28,35},{29,15}},6)
 poly(a,{{31,4},{32,4},{32,15},{31,33},{28,34},{30,14}},8)
 poly(a,{{33,15},{34,19},{35,34},{32,34}},7)
 rect(a,29,20,5,2,4);rect(a,29,25,5,1,7)
 for _,x in ipairs{23,40}do
 poly(a,{{x-5,58},{x+5,58},{x+6,66},{x+3,74},{x-3,74},{x-6,66}},1)
 poly(a,{{x-4,59},{x+4,59},{x+4,66},{x+2,72},{x-2,72},{x-4,66}},6)
 line(a,x-4,60,x-3,66,8);line(a,x-2,59,x+2,59,7);line(a,x+3,60,x+3,66,3)
 rect(a,x-3,67,7,5,9);rect(a,x-2,68,5,3,11);rect(a,x-1,69,3,2,13)
 end
 save(a,"player")
end
do
 local a=image(64,80)
 poly(a,{{31,3},{32,3},{40,24},{45,31},{62,58},{61,67},{46,61},{39,71},{24,71},{17,61},{2,67},{1,58},{18,31},{23,24}},1)
 poly(a,{{31,6},{32,6},{38,25},{43,33},{59,58},{59,63},{45,57},{37,68},{26,68},{18,57},{4,63},{4,58},{20,33},{25,25}},5)
 for _,right in ipairs{false,true}do
 local function P(points,c)local p={};for _,q in ipairs(points)do p[#p+1]={right and 63-q[1]or q[1],q[2]}end;poly(a,p,c)end
 P({{24,26},{23,38},{20,51},{6,60},{7,56},{20,33}},7)
 P({{22,31},{21,40},{9,56},{8,56},{20,34}},8)
 P({{23,43},{21,54},{15,57},{18,47}},6)
 P({{23,33},{22,45},{18,50},{18,43}},10)
 P({{8,59},{17,55},{16,59},{5,64}},3)
 end
 poly(a,{{31,5},{32,5},{36,20},{38,32},{37,49},{35,65},{28,65},{26,49},{25,32},{27,20}},6)
 poly(a,{{31,8},{31,23},{27,34},{28,48},{30,52},{29,33},{32,24}},7)
 poly(a,{{33,12},{35,23},{37,33},{36,48},{33,52}},4)
 poly(a,{{31,20},{34,25},{35,38},{32,43},{28,38},{28,25}},1)
 poly(a,{{31,22},{33,26},{33,37},{31,40},{29,37},{29,26}},11)
 line(a,30,25,30,35,13);rect(a,30,46,4,10,3);rect(a,31,48,2,6,10)
 for _,x in ipairs{16,47}do
 poly(a,{{x-5,43},{x-3,39},{x+3,39},{x+5,43},{x+5,63},{x+3,66},{x-3,66},{x-5,63}},1)
 rect(a,x-4,44,9,17,6);rect(a,x-4,44,3,15,7);rect(a,x+2,44,2,15,4)
 line(a,x-3,42,x+3,42,8);rect(a,x-3,58,7,6,9);rect(a,x-2,60,5,3,11);rect(a,x-1,61,3,2,13)
 for y=47,53,3 do rect(a,x, y,3,1,3)end
 end
 save(a,"player-swept")
end
for _,id in ipairs{"player","player-swept"}do
 local a=load(id);local amber=Image(a)
 for y=25,69 do for x=0,63 do local v=a:getPixel(x,y)
 if math.abs(x-31.5)>7 and(v==pal[6]or v==pal[7]or v==pal[8])then amber:putPixel(x,y,v==pal[8]and pal[24]or v==pal[7]and pal[23]or pal[22])end end end
 save(amber,id.."-amber")
end

print("PASS final player sources")
