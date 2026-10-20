local root=app.params.root
assert(root and root~="", "root required")
local colors = {"080f20","111d32","1c2e46","2d4660","46617a","6e899b","a6bfcb","e5f4f2","0b3948","126578","1b9bb2","41d2de","a0f1ec","271b32","50273d","87354c","bb4057","ed6470","ffb69c","3b2836","745047","ad704b","e8aa58","ffe0a0","282846","505073","8484ac","c0b8d1","464025","7e7745","bab773","f1edb3"}
local palette = {}
for i, h in ipairs(colors) do palette[i]=app.pixelColor.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end

local base=Image{fromFile=root.."/../../../../app/Assets/StarShift/Art/M8/stellar-shard.png"}
local sprite=Sprite(32,40,ColorMode.RGB)
for i=1,4 do
 local raw=Image{fromFile=root.."/raw/frame_"..string.format("%02d",i)..".png"}
 assert(raw.width==32 and raw.height==40,"canvas drift")
 local im=Image(32,40,ColorMode.RGB)
 for y=0,39 do for x=0,31 do
  local p=raw:getPixel(x,y);local mask=base:getPixel(x,y)
  -- Giữ silhouette anchor, chỉ lấy glint từ frame sinh ra và đưa về shared palette.
  if app.pixelColor.rgbaA(mask)>127 then
   if app.pixelColor.rgbaA(p)<128 then p=mask end
   local best,dist=1,1e12
   for k,q in ipairs(palette)do
    local dr=app.pixelColor.rgbaR(p)-app.pixelColor.rgbaR(q)
    local dg=app.pixelColor.rgbaG(p)-app.pixelColor.rgbaG(q)
    local db=app.pixelColor.rgbaB(p)-app.pixelColor.rgbaB(q)
    local d=dr*dr+dg*dg+db*db
    if d<dist then best=k;dist=d end
   end
   im:putPixel(x,y,palette[best])
  end
 end end
 if i>1 then sprite:newEmptyFrame() end
 sprite:newCel(sprite.layers[1],sprite.frames[i],im,Point(0,0))
 sprite.frames[i].duration=.125
 im:saveAs(root.."/frames/shimmer_"..string.format("%02d",i-1)..".png")
end
sprite:saveAs(root.."/shard-shimmer.aseprite")
sprite:close()
