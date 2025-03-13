---@diagnostic disable: need-check-nil
BasePanel:subClass("MainPanel")

-- 初始化面板 实例化对量 控件事件监听
function MainPanel:Init(name)
    self.base.Init(self, name)
    if self.isInitEvent == false then
        self:GetControl("BtnRole", "Button").onClick:AddListener(function()
            self:BtnRoleClick()
        end)
        self.isInitEvent = true
    end

end

function MainPanel:BtnRoleClick()
    BagPanel:ShowMe("BagPanel")
end
