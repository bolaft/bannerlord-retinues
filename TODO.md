- duplicate custom troop functionality for kingdom
- implement custom troop spawning in clan/kingdom holdings
- fix cloned upgrade targets sharing equipment with originals
- implement troop persistence (rewrite troop cloning?)
- implement stocks/unlocks persistence
- implement unlocking mechanics

# Refactor

/ Behaviors                         X
    CampaignBehavior.cs             X
/ UI                                .
    / Patches                       X
        / ClanScreen                X   
            Constants.cs            X
            Overrides.cs            X
            TroopsPanel.cs          X
            TroopsTab.cs            X
    / VM                            .
        / Equipment                 .
            EquipmentEditor.cs    .
            EquipmentList.cs      .
            EquipmentRow.cs       .
            EquipmentSlot.cs      .
        / Troops                    .
            TroopEditor.cs        .
            TroopList.cs          .
            TroopRow.cs           .
            TroopSkill.cs         .
            TroopUpgradeTarget.cs .
    ClanManagementMixin.cs        .
/ Utils                             X
    Config.cs                       X
    Log.cs                          X
    Reflector.cs                    X
/ Wrappers                          X
    / Objects                       X
        Character.cs                X
        Equipment.cs                X
        Item.cs                     X
    / Campaign                      X
        Clan.cs                     X
        Culture.cs                  X
/ Game                              X
    Player.cs                       X
    Setup.cs                        X
    / Objects                       X
        Character.cs                X
        Equipment.cs                X
        Item.cs                     X
    / Campaign                      X
        Clan.cs                     X
        Culture.cs                  X
