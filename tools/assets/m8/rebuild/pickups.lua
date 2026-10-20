local root=assert(app.params.root);local out=assert(app.params.out)
local function c(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function rect(im,x,y,w,h,hx)for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and yy>=0 and xx<im.width and yy<im.height then im:putPixel(xx,yy,c(hx))end end end end
local function poly(im,p,hx)
 for y=0,im.height-1 do local xs={};for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end;table.sort(xs);for i=1,#xs-1,2 do rect(im,math.ceil(xs[i]),y,math.floor(xs[i+1])-math.ceil(xs[i])+1,1,hx)end end
end
local specs={
 {'pulse-core','ui-weapon-pulse','4fb9cb'}, {'plasma-core','ui-weapon-plasma','9885c4'},
 {'homing-core','ui-weapon-homing','d0a565'}, {'missile-ammo','ui-ammo','d0a565'},
 {'extra-life','ui-life','8fbf9f'}, {'support-drone','ui-support','6eaeba'},
 {'shield-cell','ui-shield','74bfb8'}, {'overdrive','ui-overdrive','d9b15c'}
}
local function saveFrames(id,frames)
 local w,h=frames[1].width,frames[1].height;local s=Sprite(w,h,ColorMode.RGB);local strip=image(w*4,h);local seen={}
 for i,im in ipairs(frames)do if i>1 then s:newFrame()end;s:newCel(s.layers[1],i,im,Point(0,0));s.frames[i].duration=.2;strip:drawImage(im,Point((i-1)*w,0));seen[im.bytes]=true end
 local n=0;for _ in pairs(seen)do n=n+1 end;assert(n==4,id..': duplicate glint frame')
 s:saveAs(root..'/'..id..'.aseprite');s:close();frames[1]:saveAs(out..'/'..id..'.png');strip:saveAs(out..'/'..id..'-sheet.png')
end
for _,spec in ipairs(specs)do
 local im=image(64,80);local accent=spec[3]
 -- Nắp kim loại, cổ lọ và thân kính chung; icon lớn là thông tin chính.
 rect(im,22,4,20,10,'20374a');rect(im,24,4,16,3,'9db7bf');rect(im,25,8,2,4,'6f899a');rect(im,31,8,2,4,'6f899a');rect(im,37,8,2,4,'6f899a')
 rect(im,25,14,14,8,'325166');rect(im,26,15,3,6,'91b9c2')
 poly(im,{{24,20},{39,20},{50,30},{50,66},{43,74},{20,74},{13,66},{13,30}},'122538')
 poly(im,{{24,22},{39,22},{47,31},{47,64},{41,71},{22,71},{16,64},{16,31}},'658a9b')
 poly(im,{{25,24},{38,24},{44,32},{44,62},{40,67},{24,67},{19,62},{19,32}},'203e54')
 poly(im,{{19,54},{44,54},{44,62},{40,67},{24,67},{19,62}},accent)
 rect(im,16,34,2,24,'a8d4d6');rect(im,19,29,2,5,'cfe9e6');rect(im,23,69,17,2,'87aab0')
 local symbol=Image{fromFile=out..'/'..spec[2]..'.png'};symbol:resize(28,28);im:drawImage(symbol,Point(18,31))
 local frames={}
 for f=0,3 do local a=Image(im);local y=27+f*8
  rect(a,16,y,2,5,'d8efeb');rect(a,15,y+2,4,1,'effaf2')
  if f%2==0 then rect(a,43,62-f*4,2,2,'b4d5d5')end
  frames[#frames+1]=a
 end
 saveFrames(spec[1],frames)
end
-- Shard giữ crystal world identity; chỉ thêm glint nhẹ như các pickup.
local shard=Image{fromFile=root..'/salvage/stellar-shard.png'};local frames={}
for f=0,3 do local a=Image(shard);local x=10+f*3;local y=8+f*4;rect(a,x,y,2,3,'fff0c5');frames[#frames+1]=a end
saveFrames('stellar-shard',frames)
print('PASS: eight capsule+icon sheets and Shard, four subtle glint frames each')
