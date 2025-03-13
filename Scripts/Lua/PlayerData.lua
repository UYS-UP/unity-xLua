PlayerData = {}

PlayerData.equips = {}
PlayerData.props = {}
PlayerData.gems = {}

function PlayerData:Init()
    table.insert(self.equips, {id = 1, nums = 1})
    table.insert(self.equips, {id = 2, nums = 1})
    
    table.insert(self.props, {id = 3, nums = 99})
    table.insert(self.props, {id = 4, nums = 50})

    table.insert(self.gems, {id = 5, nums = 20})
    table.insert(self.gems, {id = 6, nums = 30})
end
PlayerData:Init()