local txt = ABMgr:LoadRes("json", "ItemData", typeof(TextAsset))
local itemList = Json.decode(txt.text)

ItemData = {}

for _, value in pairs(itemList) do
    ItemData[value.id] = value
end

