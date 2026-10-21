local root=assert(app.params.root);local out=assert(app.params.out)
local function c(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),#h==8 and tonumber(h:sub(7,8),16) or 255)end
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
 local im=image(64,64);local accent=spec[3]
 -- Hộp kính vuông bo góc; lòng trong suốt để icon có nhiều diện tích hơn.
 poly(im,{{10,4},{53,4},{59,10},{59,53},{53,59},{10,59},{4,53},{4,10}},'28465aa8')
 poly(im,{{11,6},{52,6},{56,11},{56,51},{51,56},{11,56},{7,51},{7,11}},accent..'80')
 poly(im,{{12,9},{51,9},{54,12},{54,50},{50,53},{12,53},{10,50},{10,12}},'84c4dc25')
 rect(im,12,6,39,2,'d6fff2d8');rect(im,7,12,2,36,'a8e8ecc8')
 rect(im,13,56,37,2,accent..'b0');rect(im,56,13,2,36,'25435da8')
 rect(im,11,11,10,2,'effff878');rect(im,11,13,2,8,'effff858')
 local symbol=Image{fromFile=out..'/'..spec[2]..'.png'};symbol:resize(42,42);im:drawImage(symbol,Point(11,11))
 local frames={}
 for f=0,3 do local a=Image(im);local x=14+f*9
  rect(a,x,5,5,2,'effff8');rect(a,x+2,4,1,4,'effff8')
  if f%2==0 then rect(a,55,43-f*7,2,3,'c3f4ec')end
  frames[#frames+1]=a
 end
 saveFrames(spec[1],frames)
end
-- Shard giữ crystal world identity; chỉ thêm glint nhẹ như các pickup.
local shard=Image{fromFile=root..'/salvage/stellar-shard.png'};local frames={}
for f=0,3 do local a=Image(shard);local x=10+f*3;local y=8+f*4;rect(a,x,y,2,3,'fff0c5');frames[#frames+1]=a end
saveFrames('stellar-shard',frames)
print('PASS: eight transparent square cases with large icons; four subtle glint frames each')
