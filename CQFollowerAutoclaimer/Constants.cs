using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQFollowerAutoclaimer
{
    static class Constants
    {
        public static string[] rewardNames = new string[] {"20 Disasters(not rewarded in game)", "50 Disasters(not rewarded in game)", "200 Disasters(not rewarded in game)", 
            "1H Energy Boost(not rewarded in game)", "4H Energy Boost(not rewarded in game)", "12H Energy Boost(not rewarded in game)", 
            "Common Followers", "Rare Followers", "Legendary Followers",
            "20 UM", "50 UM", "200 UM"};

        public static string[] names = { "flynn", "leaf", "sparks", "leprechaun", "bavah", "boor", "bylar", "adagda", "hattori", "hirate", "takeda", "hosokawa", "moak", "arigr", "dorth", 
            "rua", "arshen", "aatzar", "apontus",  "bubbles",  "dagda",  "ganah", "toth",  "sexysanta", "santaclaus", "reindeer", "christmaself", "lordofchaos", "ageror", 
            "ageum", "atr0n1x", "aauri", "arei", "aathos", "aalpha", "rigr", "hallinskidi", "hama", "alvitr", "koldis", "sigrun", "neptunius", "lordkirk", "thert", "shygu", 
            "ladyodelith", "dullahan", "jackoknight", "werewolf", "gurth", "koth", "zeth", "atzar", "xarth", "oymos", "gaiabyte", "aoyuki", "spyke", "zaytus", "petry", 
            "chroma", "pontus", "erebus", "ourea", "groth", "brynhildr", "veildur", "geror", "aural", "rudean", "undine", "ignitor", "forestdruid", "geum", "aeris", 
            "aquortis", "tronix", "taurus", "kairy", "james", "nicte", "auri", "faefyr", "ailen", "rei", "geron", "jet", "athos", "nimue", "carl", "alpha", "shaman", 
            "hunter", "bewat", "pyromancer", "rokka", "valor", "nebra", "tiny", "ladyoftwilight", "", 
            "A1", "E1", "F1", "W1", "A2", "E2", "F2", "W2", "A3", "E3", "F3", "W3", "A4", "E4", "F4", "W4", "A5", "E5", "F5", "W5", "A6", "E6", "F6", "W6", 
            "A7", "E7", "F7", "W7", "A8", "E8", "F8", "W8", "A9", "E9", "F9", "W9", "A10", "E10", "F10", "W10", "A11", "E11", "F11", "W11", "A12", "E12", "F12", "W12", 
            "A13", "E13", "F13", "W13", "A14", "E14", "F14", "W14", "A15", "E15", "F15", "W15", "A16","E16","F16","W16","A17","E17","F17","W17","A18","E18","F18","W18",
            "A19","E19","F19","W19","A20","E20","F20","W20","A21","E21","F21","W21", "A22","E22","F22","W22","A23","E23","F23","W23","A24","E24","F24","W24",
            "A25","E25","F25","W25","A26","E26","F26","W26","A27","E27","F27","W27","A28","E28","F28","W28","A29","E29","F29","W29","A30","E30","F30","W30",};

        public static int heroesInGame = Array.IndexOf(names, "ladyoftwilight") + 2;
        public static string[] heroNames = new string[] { "NULL", "NULL", "Ladyoftwilight", "Tiny", "Nebra", "Valor", "Rokka", "Pyromancer", "Bewat", "Hunter", "Shaman", "Alpha", "Carl", 
            "Nimue", "Athos", "Jet", "Geron", "Rei", "Ailen", "Faefyr", "Auri", "Nicte", "James", "Kairy", "Taurus", "Tronix", "Aquortis", "Aeris", "Geum", "Forestdruid", 
            "Ignitor", "Undine", "Rudean", "Aural", "Geror", "Veildur", "Brynhildr", "Groth", "Ourea", "Erebus", "Pontus", "Chroma", "Petry", "Zaytus", "Spyke", "Aoyuki",
            "Gaiabyte", "Oymos", "Xarth", "Atzar", "Zeth", "Koth", "Gurth", "Werewolf", "Jackoknight", "Dullahan", "Ladyodelith", "Shygu", "Thert", "Lordkirk", "Neptunius", 
            "Sigrun", "Koldis", "Alvitr", "Hama", "Hallinskidi", "Rigr", "Aalpha", "Aathos", "Arei", "Aauri", "Atr0n1x", "Ageum", "Ageror", "Lordofchaos", "Christmaself", 
            "Reindeer", "Santaclaus", "Sexysanta", "Toth", "Ganah", "Dagda", "Bubbles", "Apontus", "Aatzar", "Arshen", "Rua", "Dorth", "Arigr", "Moak", "Hosokawa", "Takeda", 
            "Hirate", "Hattori", "Adagda", "Bylar", "Boor", "Bavah", "Leprechaun", "Sparks", "Leaf", "Flynn"};
    }
}
