local root=assert(app.params.root);local out=assert(app.params.out)
local function c(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function image(w,h)return Image(w,h,ColorMode.RGB)end
local function rect(im,x,y,w,h,hx)for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and yy>=0 and xx<im.width and yy<im.height then im:putPixel(xx,yy,c(hx))end end end end
local function poly(im,p,hx)
 for y=0,im.height-1 do local xs={};for i,a in ipairs(p)do local b=p[i%#p+1];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2])end end;table.sort(xs);for i=1,#xs-1,2 do rect(im,math.ceil(xs[i]),y,math.floor(xs[i+1])-math.ceil(xs[i])+1,1,hx)end end
end
local function save(im,id)local s=Sprite(im.width,im.height,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root..'/'..id..'.aseprite');s:saveAs(out..'/'..id..'.png');s:close()end
local function states(im,id)
 save(im,id)
 for stage=1,2 do
  local a=Image(im);local radius=math.floor(im.width*(stage==1 and .10 or .18));local cy=math.floor(im.height*.57)
  for y=cy-radius,cy+radius do
   local edge=nil;for x=0,im.width-1 do if app.pixelColor.rgbaA(a:getPixel(x,y))>0 then edge=x;break end end
   if edge then local depth=math.floor(radius*(1-math.abs(y-cy)/(radius+1)));for x=edge,edge+depth do a:putPixel(x,y,0)end;if edge+depth+1<im.width then a:putPixel(edge+depth+1,y,c('bdcbd2'))end end
  end
  save(a,id..(stage==1 and '-damaged' or '-critical'))
 end
end
local function plate(warden)
 local a=image(96,96);local dark=warden and '232b48' or '302737';local mid=warden and '4b5d84' or '67526d';local light=warden and 'aabed6' or 'b8a1b8'
 poly(a,{{20,5},{75,5},{89,19},{89,76},{77,89},{19,89},{6,76},{6,19}},'101b2b')
 poly(a,{{21,8},{74,8},{85,20},{85,74},{75,85},{21,85},{10,74},{10,20}},mid)
 poly(a,{{21,8},{74,8},{80,14},{23,14},{16,23},{16,71},{10,74},{10,20}},light)
 poly(a,{{23,18},{73,18},{78,24},{78,70},{70,78},{24,78},{18,70},{18,24}},dark)
 for _,x in ipairs{22,72}do rect(a,x,24,3,9,light);rect(a,x,61,3,9,mid)end
 return a
end
for _,family in ipairs{'vanguard','warden'}do
 local warden=family=='warden';local accent=warden and '6dded3' or 'de7680';local hot=warden and 'ddfff1' or 'ffd3a7'
 for _,kind in ipairs(warden and {'beam-node','drone-node','plasma-node','core'} or {'beam-pod','missile-pod','engine','reactor'})do
  local a=plate(warden)
  if kind:find('beam')then
   for _,x in ipairs{30,54}do rect(a,x,25,12,65,'111d2d');rect(a,x+2,28,8,58,'58768d');rect(a,x+2,29,3,56,'bacbd7');rect(a,x+2,77,8,5,accent)end
   rect(a,26,20,44,6,accent)
  elseif kind=='missile-pod' then
   for _,x in ipairs{24,42,60}do for _,y in ipairs{22,43,64}do
    rect(a,x,y,13,17,'101b2b');poly(a,{{x+6,y+2},{x+10,y+8},{x+10,y+13},{x+3,y+13},{x+3,y+8}},'c5d2d9');poly(a,{{x+6,y+2},{x+10,y+8},{x+3,y+8}},accent)
   end end
  elseif kind=='drone-node' then
   rect(a,24,24,48,45,'0e1c2a');rect(a,26,26,44,6,'6293a8');rect(a,26,62,44,5,'6293a8')
   for _,x in ipairs{32,45,58}do poly(a,{{x,39},{x+4,47},{x+8,50},{x+7,55},{x+4,52},{x+1,55},{x,50}},accent)end
   rect(a,38,72,20,3,hot)
  elseif kind=='engine' then
   for _,x in ipairs{24,43,62}do rect(a,x,28,11,46,'131d29');rect(a,x+2,31,3,31,'a991ae');rect(a,x+2,65,7,5,'e49a6f');rect(a,x+3,66,5,2,hot)end
  elseif kind=='plasma-node' then
   poly(a,{{48,22},{67,35},{67,55},{48,69},{28,55},{28,35}},accent);poly(a,{{48,28},{60,38},{60,52},{48,62},{35,52},{35,38}},'345b70');poly(a,{{48,35},{55,41},{55,49},{48,55},{41,49},{41,41}},hot)
   for _,x in ipairs{24,69}do rect(a,x,33,3,26,accent)end
  else
   poly(a,{{48,20},{72,34},{72,61},{48,76},{24,61},{24,34}},accent);poly(a,{{48,26},{65,38},{65,57},{48,69},{31,57},{31,38}},'26384d');poly(a,{{48,34},{57,41},{57,54},{48,61},{39,54},{39,41}},hot)
   rect(a,46,18,4,12,hot);rect(a,46,66,4,12,hot)
  end
  states(a,family..'-'..kind)
 end
 local wing=image(256,112);local base=warden and '354263' or '4b354d';local mid=warden and '637a9e' or '84617e';local high=warden and 'c0d4df' or 'c9a6bf'
 poly(wing,{{4,33},{65,8},{176,12},{250,39},{234,83},{161,103},{59,85},{4,62}},'111c2d')
 poly(wing,{{8,35},{66,12},{174,16},{245,42},{231,79},{161,98},{61,80},{8,59}},base)
 poly(wing,{{12,36},{67,15},{173,20},{213,36},{68,41},{12,55}},mid)
 poly(wing,{{67,15},{173,20},{189,26},{68,22},{17,43},{13,40}},high)
 for _,x in ipairs{48,83,118,153,188}do
  poly(wing,{{x,48},{x+23,44},{x+29,70},{x+9,78}},mid);rect(wing,x+7,52,12,2,high);rect(wing,x+11,65,11,4,accent)
 end
 states(wing,family..'-wing')
 local truss=image(192,48);poly(truss,{{3,14},{184,6},{189,28},{10,43}},base)
 for x=12,170,28 do poly(truss,{{x,17},{x+17,15},{x+25,29},{x+8,33}},mid);rect(truss,x+5,18,10,2,high)end
 states(truss,family..'-truss')
end
-- Cargo craft: kẹp và động cơ hai bên, hộp hàng amber là silhouette chính.
local cargo=image(112,80)
for _,x in ipairs{8,89}do
 poly(cargo,{{x,13},{x+10,8},{x+14,15},{x+14,60},{x+7,70},{x,60}},'25384a')
 rect(cargo,x+2,16,9,39,'c8d7d8');rect(cargo,x+3,19,3,31,'f2f2da');rect(cargo,x+3,57,7,7,'132436')
end
rect(cargo,19,28,74,6,'587689');rect(cargo,19,55,74,5,'587689')
local crate=image(56,56)
poly(crate,{{8,5},{47,5},{53,13},{53,46},{46,53},{8,53},{2,46},{2,13}},'253445')
poly(crate,{{9,8},{46,8},{49,14},{49,44},{44,49},{10,49},{6,44},{6,14}},'a06b34')
poly(crate,{{10,9},{45,9},{47,16},{8,16}},'f6d58c');rect(crate,8,18,40,25,'d6a45a');rect(crate,11,20,4,21,'edc480');rect(crate,38,20,8,22,'b87d3b')
rect(crate,24,8,8,41,'f0e4bc');rect(crate,9,29,38,6,'596c74');rect(crate,24,28,8,9,'203747');rect(crate,26,30,4,4,'8edbd5')
cargo:drawImage(crate,Point(28,14));states(cargo,'supply-drone');save(crate,'legacy-crate')
print('Module motifs and cargo target authored')
