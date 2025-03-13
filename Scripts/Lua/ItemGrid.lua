Object:subClass("ItemGrid")
ItemGrid.obj = nil
ItemGrid.imageIcon = nil
ItemGrid.value = nil

function ItemGrid:Init(father, posX, posY)
    self.obj = ABMgr:LoadRes("ui", "ItemGrid");
    self.obj.transform:SetParent(father, false)
    self.obj.transform.localPosition = Vector3(posX, posY, 0)
    self.imageIcon = self.obj.transform:Find("ImageIcon"):GetComponent(typeof(Image))
    self.value = self.obj.transform:Find("Value"):GetComponent(typeof(TextMeshProUGUI))
end

function ItemGrid:InitData(data)
    local itemData = ItemData[data.id]
    local strs = string.split(itemData.icon, "_")
    local spriteAltas = ABMgr:LoadRes("ui", strs[1], typeof(SpriteAtlas))
    self.imageIcon.sprite = spriteAltas:GetSprite(strs[2])
    self.value.text = data.nums
end

function ItemGrid:Destroy()
    GameObject.Destroy(self.obj)
    self.obj = nil
end

