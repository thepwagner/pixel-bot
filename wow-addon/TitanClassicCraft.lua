local size = 1;

local rc = LibStub("LibRangeCheck-2.0")

local index = GetCurrentResolution();
local currentResolution = select(index, GetScreenResolutions());  -- this will return a value in the format 1920x1080
local resWidth, resHeight = strsplit("x", currentResolution, 2)
local scale = GetScreenWidth() / resWidth 

_, _, classIndex = UnitClass("player");
local myBuffs = { "Food", "Drink"};
if (classIndex == 8) then
    -- mage
    table.insert(myBuffs, "Arcane Intellect")
    table.insert(myBuffs, "Frost Armor")
    table.insert(myBuffs, "Mana Shield")
    table.insert(myBuffs, "Ice Barrier")
elseif (classIndex == 5) then
     -- priest
     table.insert(myBuffs, "Shadowform")
     table.insert(myBuffs, "Power Word: Fortitude")
     table.insert(myBuffs, "Inner Fire")
     table.insert(myBuffs, "Power Word: Shield")
end


local parent = CreateFrame("frame", "Recount", UIParent);
parent:SetSize(5 * size, 5 * size);  -- Width, Height
parent:SetPoint("TOPLEFT", 0, 0);
parent:SetScale(scale);
parent.t = parent:CreateTexture();
parent.t:SetColorTexture(0, 0, 0, 0);
parent.t:SetAllPoints(parent);
parent:SetFrameStrata("TOOLTIP");

-- (0,0) - (health, mana, class)
local playerFrame = CreateFrame("frame", "", parent);
playerFrame:SetSize(size, size);
playerFrame:SetPoint("TOPLEFT", 0, 0);
playerFrame.t = playerFrame:CreateTexture();
playerFrame.t:SetColorTexture(0, 0, 0, 1);
playerFrame.t:SetAllPoints(playerFrame);
playerFrame:Show();
playerFrame:RegisterEvent("PLAYER_ENTERING_WORLD");
playerFrame:RegisterEvent("PLAYER_REGEN_DISABLED")
playerFrame:RegisterEvent("PLAYER_REGEN_ENABLED")
playerFrame:RegisterUnitEvent("UNIT_HEALTH", "player");
playerFrame:RegisterUnitEvent("UNIT_POWER_UPDATE", "player");
playerFrame:RegisterUnitEvent("UNIT_POWER_UPDATE", "player");


local function updatePlayer(self, event)
	local health = UnitHealth("player");		
	local maxHealth = UnitHealthMax("player");
	local percHealth = ceil((health / maxHealth) * 255);
    
    local power = UnitPower("player");		
	local maxPower  = UnitPowerMax("player");
    local percPower = ceil((power / maxPower) * 255);

    local combat = UnitAffectingCombat("player");
    local blue = classIndex / 24;
    if (combat) then
        blue = blue + 0.5;
    end
    playerFrame.t:SetColorTexture(percHealth / 255, percPower / 255, blue, 1);
end
playerFrame:SetScript("OnEvent", updatePlayer);

-- (0,1) - (target, health, 0)
local targetFrame = CreateFrame("frame", "", parent);
targetFrame:SetSize(size, size);
targetFrame:SetPoint("TOPLEFT", size, 0);
targetFrame.t = targetFrame:CreateTexture();
targetFrame.t:SetColorTexture(0, 0, 0, 1);
targetFrame.t:SetAllPoints(targetFrame);
targetFrame:Show();
targetFrame:RegisterUnitEvent("UNIT_TARGET", "player");
targetFrame:RegisterUnitEvent("UNIT_HEALTH", "target");

local targetTimer;

local function updateTarget(self, event)
    if (targetTimer) then
        targetTimer:Cancel()
        targetTimer = nil;
    end

    local red = 0;
    local green = 0;
    if (UnitExists("target") and not UnitIsFriend("player","target")) then
        red = 1;

        local health = UnitHealth("target");		
	    local maxHealth = UnitHealthMax("target");
        local percHealth = ceil((health / maxHealth) * 255);
        green = percHealth / 255;

        targetTimer = C_Timer.NewTimer(0.75, updateTarget);
    end

    local blue = 0;
    local minRange, maxRange = rc:GetRange('target')
    if minRange then
        blue = minRange / 45;
        if not maxRange then
            blue = 1
        end
    end

    targetFrame.t:SetColorTexture(red, green, blue, 1);
end
targetFrame:SetScript("OnEvent", updateTarget);

-- (1,*) - player buffs
local myBuffsCount = 0;
local myBuffsIndex = {};
for _, buffId in pairs(myBuffs) do
    myBuffsCount = myBuffsCount + 1
end

local myBuffFrames = {};
for i=1,ceil(myBuffsCount/3) do
    myBuffFrames[i] = CreateFrame("frame","", parent);
    myBuffFrames[i]:SetSize(size, size);
    myBuffFrames[i]:SetPoint("TOPLEFT", (i-1)* size, -1 * size);
    myBuffFrames[i].t = myBuffFrames[i]:CreateTexture();
    myBuffFrames[i].t:SetColorTexture(1, 1, 1, 1);
	myBuffFrames[i].t:SetAllPoints(myBuffFrames[i]);
    myBuffFrames[i]:Show();
    myBuffFrames[i]:RegisterEvent("PLAYER_ENTERING_WORLD");
    myBuffFrames[i]:RegisterUnitEvent("UNIT_AURA","player");
end

local function updateMyBuffs(self, event)
    -- Index time remaining by buff name:
    local buffs = {};
    for i=1,40 do
        local name, buff, count, buffType, duration, expirationTime, isMine, isStealable, _, spellId = UnitBuff("player", i);
        if name then
            local remainingTime;
            if (expirationTime == 0) then
                remainingTime = 180;
            else
                remainingTime = -1*(GetTime()-expirationTime);
            end
            if remainingTime > 180 then
                remainingTime = 180;
            end
            --print(name.." "..remainingTime)
            buffs[name] = remainingTime / 180;
        end
    end

    for i=0,ceil(myBuffsCount/3)-1 do
        -- print("c"..i);
        local red = 0;
        if myBuffs[i*3+1] then
            if buffs[myBuffs[i*3+1]] then
                red = buffs[myBuffs[i*3+1]];
            end
        end

        local green = 0;
        if myBuffs[i*3+2] then
            if buffs[myBuffs[i*3+2]] then
                green = buffs[myBuffs[i*3+2]];
            end
        end

        local blue = 0;
        if myBuffs[i*3+3] then
            if buffs[myBuffs[i*3+3]] then
                blue = buffs[myBuffs[i*3+3]];
            end
        end

        --print("c"..i.." r"..red.." g"..green.." b"..blue)
        myBuffFrames[i+1].t:SetColorTexture(red, green, blue, 1);
    end
end
myBuffFrames[1]:SetScript("OnEvent", updateMyBuffs);
