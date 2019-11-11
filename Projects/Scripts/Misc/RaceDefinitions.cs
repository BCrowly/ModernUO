using System.Linq;

namespace Server.Misc
{
  public class RaceDefinitions
  {
    public static void Configure()
    {
      /* Here we configure all races. Some notes:
       *
       * 1) The first 32 races are reserved for core use.
       * 2) Race 0x7F is reserved for core use.
       * 3) Race 0xFF is reserved for core use.
       * 4) Changing or removing any predefined races may cause server instability.
       */

      RegisterRace(new Human(0, 0));
      RegisterRace(new Elf(1, 1));
      RegisterRace(new Gargoyle(2, 2));
    }

    public static void RegisterRace(Race race)
    {
      Race.Races[race.RaceIndex] = race;
      Race.AllRaces.Add(race);
    }

    private class Human : Race
    {
      public Human(int raceID, int raceIndex)
        : base(raceID, raceIndex, "Human", "Humans", 400, 401, 402, 403, Expansion.None)
      {
      }

      public override bool ValidateHair(bool female, int itemID) =>
        itemID == 0 || (!female || itemID != 0x2048) && (female || itemID != 0x2046) &&
        (itemID >= 0x203B && itemID <= 0x203D || itemID >= 0x2044 && itemID <= 0x204A);

      public override int RandomHair(bool female) //Random hair doesn't include baldness
      {
        return Utility.Random(9) switch
        {
          0 => 0x203B, //Short
          1 => 0x203C, //Long
          2 => 0x203D, //Pony Tail
          3 => 0x2044, //Mohawk
          4 => 0x2045, //Pageboy
          5 => 0x2047, //Afro
          6 => 0x2049, //Pig tails
          7 => 0x204A, //Krisna
          _ => (female ? 0x2046 : 0x2048)
        };
      }

      public override bool ValidateFacialHair(bool female, int itemID)
      {
        if (itemID == 0)
          return true;

        if (female)
          return false;

        if (itemID >= 0x203E && itemID <= 0x2041)
          return true;

        if (itemID >= 0x204B && itemID <= 0x204D)
          return true;

        return false;
      }

      public override int RandomFacialHair(bool female)
      {
        if (female)
          return 0;

        int rand = Utility.Random(7);

        return (rand < 4 ? 0x203E : 0x2047) + rand;
      }

      public override int ClipSkinHue(int hue) => hue < 1002 ? 1002 : hue > 1058 ? 1058 : hue;

      public override int RandomSkinHue() => Utility.Random(1002, 57) | 0x8000;

      public override int ClipHairHue(int hue) => hue < 1102 ? 1102 : hue > 1149 ? 1149 : hue;

      public override int RandomHairHue() => Utility.Random(1102, 48);
    }

    private class Elf : Race
    {
      private static readonly int[] m_SkinHues =
      {
        0x0BF, 0x24D, 0x24E, 0x24F, 0x353, 0x361, 0x367, 0x374,
        0x375, 0x376, 0x381, 0x382, 0x383, 0x384, 0x385, 0x389,
        0x3DE, 0x3E5, 0x3E6, 0x3E8, 0x3E9, 0x430, 0x4A7, 0x4DE,
        0x51D, 0x53F, 0x579, 0x76B, 0x76C, 0x76D, 0x835, 0x903
      };

      private static readonly int[] m_HairHues =
      {
        0x034, 0x035, 0x036, 0x037, 0x038, 0x039, 0x058, 0x08E,
        0x08F, 0x090, 0x091, 0x092, 0x101, 0x159, 0x15A, 0x15B,
        0x15C, 0x15D, 0x15E, 0x128, 0x12F, 0x1BD, 0x1E4, 0x1F3,
        0x207, 0x211, 0x239, 0x251, 0x26C, 0x2C3, 0x2C9, 0x31D,
        0x31E, 0x31F, 0x320, 0x321, 0x322, 0x323, 0x324, 0x325,
        0x326, 0x369, 0x386, 0x387, 0x388, 0x389, 0x38A, 0x59D,
        0x6B8, 0x725, 0x853
      };

      public Elf(int raceID, int raceIndex)
        : base(raceID, raceIndex, "Elf", "Elves", 605, 606, 607, 608, Expansion.ML)
      {
      }

      public override bool ValidateHair(bool female, int itemID) =>
        itemID == 0 || (!female || itemID != 0x2FCD && itemID != 0x2FBF) &&
        (female || itemID != 0x2FCC && itemID != 0x2FD0) &&
        (itemID >= 0x2FBF && itemID <= 0x2FC2 || itemID >= 0x2FCC && itemID <= 0x2FD1);

        if (female && (itemID == 0x2FCD || itemID == 0x2FBF) || !female && (itemID == 0x2FCC || itemID == 0x2FD0))
          return false;

        if (itemID >= 0x2FBF && itemID <= 0x2FC2)
          return true;

        if (itemID >= 0x2FCC && itemID <= 0x2FD1)
          return true;

        return false;
      }

      public override int RandomHair(bool female) //Random hair doesn't include baldness
      {
        return Utility.Random(8) switch
        {
          0 => 0x2FC0, //Long Feather
          1 => 0x2FC1, //Short
          2 => 0x2FC2, //Mullet
          3 => 0x2FCE, //Knob
          4 => 0x2FCF, //Braided
          5 => 0x2FD1, //Spiked
          6 => (female ? 0x2FCC : 0x2FBF), //Flower or Mid-long
          _ => (female ? 0x2FD0 : 0x2FCD)
        };
      }

      public override bool ValidateFacialHair(bool female, int itemID) => itemID == 0;

      public override int RandomFacialHair(bool female) => 0;

      public override int ClipSkinHue(int hue)
      {
        for (int i = 0; i < m_SkinHues.Length; i++)
          if (m_SkinHues[i] == hue)
            return hue;

        return m_SkinHues[0];
      }

      public override int RandomSkinHue() => m_SkinHues[Utility.Random(m_SkinHues.Length)] | 0x8000;

      public override int RandomSkinHue() => m_SkinHues[Utility.Random(m_SkinHues.Length)] | 0x8000;

      public override int ClipHairHue(int hue) => m_HairHues.Any(t => t == hue) ? hue : m_HairHues[0];

      public override int RandomHairHue() => m_HairHues[Utility.Random(m_HairHues.Length)];
    }

    #region SA

    private class Gargoyle : Race
    {
      // Todo Finish body hues
      private static readonly int[] m_BodyHues =
      {
        0x86DB, 0x86DC, 0x86DD, 0x86DE,
        0x86DF, 0x86E0, 0x86E1, 0x86E2,
        0x86E3, 0x86E4, 0x86E5, 0x86E6
        // 0x, 0x, 0x, 0x, // 86E7/86E8/86E9/86EA?
        // 0x, 0x, 0x, 0x, // 86EB/86EC/86ED/86EE?
        // 0x86F3, 0x86DB, 0x86DC, 0x86DD
      };

      private static readonly int[] m_HornHues =
      {
        0x709, 0x70B, 0x70D, 0x70F, 0x711, 0x763,
        0x765, 0x768, 0x76B, 0x6F3, 0x6F1, 0x6EF,
        0x6E4, 0x6E2, 0x6E0, 0x709, 0x70B, 0x70D
      };

      public Gargoyle(int raceID, int raceIndex)
        : base(raceID, raceIndex, "Gargoyle", "Gargoyles", 666, 667, 402, 403, Expansion.SA)
      {
      }

      public override bool ValidateHair(bool female, int itemID) =>
        female == false
          ? itemID >= 0x4258 && itemID <= 0x425F
          : itemID == 0x4261 || itemID == 0x4262 || itemID >= 0x4273 && itemID <= 0x4275 || itemID == 0x42B0 ||
            itemID == 0x42B1 || itemID == 0x42AA || itemID == 0x42AB;

      public override int RandomHair(bool female)
      {
        if (Utility.Random(9) == 0)
          return 0;
        if (!female)
          return 0x4258 + Utility.Random(8);
        return Utility.Random(9) switch
        {
          0 => 0x4261,
          1 => 0x4262,
          2 => 0x4273,
          3 => 0x4274,
          4 => 0x4275,
          5 => 0x42B0,
          6 => 0x42B1,
          7 => 0x42AA,
          8 => 0x42AB,
          _ => 0
        };
      }

      public override bool ValidateFacialHair(bool female, int itemID) => !female && itemID >= 0x42AD && itemID <= 0x42B0;

      public override int RandomFacialHair(bool female) => female ? 0 : Utility.RandomList(0, 0x42AD, 0x42AE, 0x42AF, 0x42B0);

      public override int ClipSkinHue(int hue) => hue;

      public override int RandomSkinHue() => m_BodyHues[Utility.Random(m_BodyHues.Length)] | 0x8000;

      public override int RandomSkinHue() => m_BodyHues[Utility.Random(m_BodyHues.Length)] | 0x8000;

      public override int ClipHairHue(int hue) => m_HornHues.Any(t => t == hue) ? hue : m_HornHues[0];

      public override int RandomHairHue() => m_HornHues[Utility.Random(m_HornHues.Length)];
    }

    #endregion
  }
}
