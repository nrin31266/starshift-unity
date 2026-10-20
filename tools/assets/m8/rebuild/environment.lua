local root=assert(app.params.root);local out=assert(app.params.out)
local function color(h)return app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
for _,region in ipairs{'astra','crimson'}do
 local palette=region=='astra' and {'080f20','0b182a','102338','19334a','244359'} or {'080f20','191327','291b35','3c2546','51334f'}
 local im=Image(768,336,ColorMode.RGB)
 local blobs=region=='astra' and {{90,225,240,120},{690,48,170,100}} or {{105,60,250,130},{650,260,260,120},{610,40,140,75}}
 for y=0,335 do for x=0,767 do
  local value=0
  for _,b in ipairs(blobs)do
   local dx,dy=(x-b[1])/b[3],(y-b[2])/b[4]
   local noise=math.sin(x*.031+y*.017)*.12+math.sin(x*.061-y*.047)*.08+math.sin(x*.13+y*.093)*.025
   value=math.max(value,1-math.sqrt(dx*dx+dy*dy)+noise)
  end
  local edge=math.min(x/20,(767-x)/20,y/20,(335-y)/20,1)
  local index=1+math.max(0,math.min(4,math.floor(value*edge*5)))
  im:putPixel(x,y,color(palette[index]))
 end end
 -- Bụi xa mờ, không dùng chấm trắng sáng trông như projectile.
 for i=1,45 do local x=(i*137+53)%748+10;local y=(i*71+29)%316+10
  if x<260 or x>550 then im:putPixel(x,y,color(palette[3]));if i%4==0 then im:putPixel(x+1,y,color(palette[2]))end end
 end
 local id=region=='astra' and 'background' or 'crimson-background';local s=Sprite(768,336,ColorMode.RGB);s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(root..'/'..id..'.aseprite');s:saveAs(out..'/'..id..'.png');s:close()
end
-- Debris riêng từng region dùng chung alpha sheet; chuyển động do runtime parallax.
local dust=Image(128,64,ColorMode.RGB)
for i=1,14 do local x=(i*37)%120+4;local y=(i*23)%56+4
 for yy=y,y+1 do for xx=x,x+2 do dust:putPixel(xx,yy,color(i%2==0 and '3b5361' or '586575'))end end
end
local s=Sprite(128,64,ColorMode.RGB);s:newCel(s.layers[1],1,dust,Point(0,0));s:saveAs(root..'/parallax-debris.aseprite');s:saveAs(out..'/parallax-debris.png');s:close()
