package.cpath = package.cpath .. ';C:/Users/YS/AppData/Roaming/JetBrains/Rider2023.3/plugins/EmmyLua/debugger/emmy/windows/x64/?.dll'
local dbg = require('emmy_core')
dbg.tcpListen('localhost', 9966)

require("InitClass") -- 初始化所有类别名
require("ItemData")
require("PlayerData")
require("BasePanel")
require("MainPanel")
require("BagPanel")
require("ItemGrid")

MainPanel:ShowMe("MainPanel")