---@diagnostic disable: need-check-nil
BasePanel:subClass("BagPanel")

BagPanel.Content = nil

BagPanel.items = {}        -- 显示当前的格子 

BagPanel.currentType = -1



-- 初始化面板 实例化对量 控件事件监听
function BagPanel:Init(name)
    self.base.Init(self, name)
    if self.isInitEvent == false then
        self.Content = self:GetControl("SVBag", "ScrollRect").transform:Find("Viewport"):Find("Content")

        self:GetControl("BtnClose", "Button").onClick:AddListener(function()
            self:BtnCloseClick()
        end)
        self:GetControl("TogEquip", "Toggle").onValueChanged:AddListener(function(value)
            if value == true then
                self:ChangeType(1)
            end
        end)    
        self:GetControl("TogProp", "Toggle").onValueChanged:AddListener(function(value)
            if value == true then
                self:ChangeType(2)
            end
        end)
        self:GetControl("TogGem", "Toggle").onValueChanged:AddListener(function(value)
            if value == true then
                self:ChangeType(3)
            end
        end)
    end

end

function BagPanel:CreateItem(nowItems)
    for i = 1, #nowItems do
        local grid = ItemGrid:new()
        grid:Init(self.Content, (i-1)%4*115, math.floor((i-1)/4)*115)
        grid:InitData(nowItems[i])
        table.insert(self.items, grid)
    end
end

function BagPanel:ChangeType(type)
    if self.nowType == type then
        return
    end
    for i = 1, #self.items do
        self.items[i]:Destroy()
    end
    self.items = {}
    local nowItems = nil
    if type == 1 then
        nowItems = PlayerData.equips
    elseif type == 2 then
        nowItems = PlayerData.props
        
    else
        nowItems = PlayerData.gems
    end
    self:CreateItem(nowItems)
end


-- 显示自己
function BagPanel:ShowMe(name)
    self.base.ShowMe(self, name)
    if self.currentType == -1 then
        self:ChangeType(1)
    end
end


function BagPanel:BtnCloseClick()
    BagPanel:HindMe()
    MainPanel:ShowMe("MainPanel")
end





