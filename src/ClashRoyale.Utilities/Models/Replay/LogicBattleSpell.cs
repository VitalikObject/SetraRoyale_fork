﻿using Newtonsoft.Json;

namespace ClashRoyale.Utilities.Models.Replay
{
    public class LogicBattleSpell
    {
        [JsonProperty("d")] public int Id { get; set; }
        [JsonProperty("l")] public int Level { get; set; }
    }
}