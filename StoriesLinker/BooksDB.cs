using System.Collections.Generic;

public enum EBookID
{
    War,
    Fantasy,
    Jungle
}

public enum WarBookCharacter
{
    Tomash,
    Teller,
    Gunn,
    Gerostrat,
    Abraham,
    Frederika,
    Karlos,
    Main,
    George,
    Dansaran,
    Vukashyn,
    Arseniy,
    Sec_Man_Barman,
    Sec_Man_Crusader_Andrzej_Prisoner,
    Sec_Man_Crusader_Andrzej_Weapon,
    Sec_Man_Crusader_Common,
    Sec_Man_Crusader_Sergeant,
    Sec_Man_Marine_Common,
    Sec_Man_Marine_Corporal,
    Sec_Man_Marine_Pilot,
    Sec_Man_Marine_Sergeant,
    Sec_Man_Marine_Wounded,
    Sec_Man_Terrorist_Common,
    Sec_Man_Terrorist_Guard1Weapon,
    Sec_Man_Terrorist_Guard2Weapon,
    Sec_Man_Terrorist_Jean_Jacques,
    Sec_Man_Terrorist_Patrol,
    Sec_Man_Terrorist_Sleep,
    Sec_Man_Terrorist_TruckDriver,
    Sec_Man_Terrorist_WatchmanNoWeapon,
    Sec_Man_Terrorist_WatchmanWeapon,
    Sec_Man_Terrorist_WeaponDealer,
    Sec_Man_TerroristHOF_Commandant,
    Sec_Man_TerroristHOF_Warlord
}

public enum WarBookLocation
{
    BazaTersio,
    BazaUni,
    BazaUSA,
    BazaDlani,
    BazaTerrGory,
    Cityeace,
    CityWar,
    DesertRoad,
    MountainRoad,
    Cave,
    CaveSklad,
    Room,
    Cabinet,
    Helicopter,
    Bar,
    Dining,
    Lab,
    Cave2,
    MountainRoad2,
    MountainRoadNight,
    MountainRoadNight2,
    BazaTerGoryNight,
    BazaTersioNight,
    BazaDlaniNight
}


public static class WarBook
{
    public static Dictionary<string, WarBookCharacter> Characters;
    public static Dictionary<WarBookCharacter, string> ChAtalsMathcing;
    public static Dictionary<WarBookCharacter, string> ChClothesVariablesMathcing;

    public static Dictionary<string, WarBookLocation> Locations;
    public static Dictionary<WarBookLocation, string> LocSpriteMatching;
    public static Dictionary<WarBookLocation, string> LocSoundMatching;

    public static string[] Currencies = new string[] { "keys", "premium", "authority", "money", "popularity" };

    public static void Init()
    {
        Characters = new Dictionary<string, WarBookCharacter>();

        Characters.Add("Томаш Зарецки", WarBookCharacter.Tomash);
        Characters.Add("Рассказчик", WarBookCharacter.Teller);
        Characters.Add("Джеймс Ганн", WarBookCharacter.Gunn);
        Characters.Add("Герострат", WarBookCharacter.Gerostrat);
        Characters.Add("Генерал Абрахам Валленштейн", WarBookCharacter.Abraham);
        Characters.Add("Фредерика Ридель", WarBookCharacter.Frederika);
        Characters.Add("Карлос Кастильо де Ромеро", WarBookCharacter.Karlos);
        Characters.Add("Игрок", WarBookCharacter.Main);
        Characters.Add("Джордж О’Доэрти", WarBookCharacter.George);
        Characters.Add("Дансаран Батожабай", WarBookCharacter.Dansaran);
        Characters.Add("Вукашин Йованович", WarBookCharacter.Vukashyn);
        Characters.Add("Арсений Козлов", WarBookCharacter.Arseniy);

        Characters.Add("Анджей", WarBookCharacter.Sec_Man_Crusader_Andrzej_Weapon);
        Characters.Add("Анджей2", WarBookCharacter.Sec_Man_Crusader_Andrzej_Prisoner);
        Characters.Add("Бармен", WarBookCharacter.Sec_Man_Barman);
        Characters.Add("Водитель грузовика", WarBookCharacter.Sec_Man_Terrorist_TruckDriver);
        Characters.Add("Второй охранник", WarBookCharacter.Sec_Man_Terrorist_Guard2Weapon);
        Characters.Add("Жан Жак", WarBookCharacter.Sec_Man_Terrorist_Jean_Jacques);
        Characters.Add("Капрал", WarBookCharacter.Sec_Man_Marine_Corporal);
        Characters.Add("Караульный", WarBookCharacter.Sec_Man_Terrorist_WatchmanWeapon);
        Characters.Add("Комендант базы", WarBookCharacter.Sec_Man_TerroristHOF_Commandant);
        Characters.Add("Неокрестоносец", WarBookCharacter.Sec_Man_Crusader_Common);
        Characters.Add("Охранник", WarBookCharacter.Sec_Man_Terrorist_Guard1Weapon);
        Characters.Add("Пилот", WarBookCharacter.Sec_Man_Marine_Pilot);
        Characters.Add("Полевой командир Длани", WarBookCharacter.Sec_Man_TerroristHOF_Warlord);
        Characters.Add("Раненый морпех", WarBookCharacter.Sec_Man_Marine_Wounded);
        Characters.Add("Сержант", WarBookCharacter.Sec_Man_Marine_Sergeant);
        Characters.Add("Сержант неокрестоносцев", WarBookCharacter.Sec_Man_Crusader_Sergeant);
        Characters.Add("Солдат США", WarBookCharacter.Sec_Man_Marine_Common);
        Characters.Add("Спящий террорист", WarBookCharacter.Sec_Man_Terrorist_Sleep);
        Characters.Add("Террорист", WarBookCharacter.Sec_Man_Terrorist_Common);
        Characters.Add("Террорист-патрульный", WarBookCharacter.Sec_Man_Terrorist_Patrol);
        Characters.Add("Террорист-часовой", WarBookCharacter.Sec_Man_Terrorist_WatchmanNoWeapon);
        Characters.Add("Торговец оружием", WarBookCharacter.Sec_Man_Terrorist_WeaponDealer);


        ChAtalsMathcing = new Dictionary<WarBookCharacter, string>();

        ChAtalsMathcing.Add(WarBookCharacter.Abraham, "War_Abraham+Arseniy");
        ChAtalsMathcing.Add(WarBookCharacter.Arseniy, "War_Abraham+Arseniy");
        ChAtalsMathcing.Add(WarBookCharacter.Dansaran, "War_Dansaran");
        ChAtalsMathcing.Add(WarBookCharacter.Frederika, "War_Frederika+Karlos");
        ChAtalsMathcing.Add(WarBookCharacter.Karlos, "War_Frederika+Karlos");
        ChAtalsMathcing.Add(WarBookCharacter.Tomash, "War_Gunn+Tomash");
        ChAtalsMathcing.Add(WarBookCharacter.Gunn, "War_Gunn+Tomash");
        ChAtalsMathcing.Add(WarBookCharacter.Main, "War_Main+Gerostat");
        ChAtalsMathcing.Add(WarBookCharacter.Gerostrat, "War_Main+Gerostat");
        ChAtalsMathcing.Add(WarBookCharacter.George, "War_George");
        ChAtalsMathcing.Add(WarBookCharacter.Vukashyn, "War_Vukashyn");

        ChClothesVariablesMathcing = new Dictionary<WarBookCharacter, string>();

        ChClothesVariablesMathcing.Add(WarBookCharacter.Arseniy, "Arsen_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Karlos, "Carlos_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Dansaran, "Dansaran_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Frederika, "Freddy_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Abraham, "General_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Gerostrat, "Gerostrat_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Main, "Hero_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.George, "Jorj_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Gunn, "Serg_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Tomash, "Tomash_dress");
        ChClothesVariablesMathcing.Add(WarBookCharacter.Vukashyn, "Wuk_dress");

        LocSpriteMatching = new Dictionary<WarBookLocation, string>();

        LocSpriteMatching.Add(WarBookLocation.BazaTersio, "warLocBaseUniSword_6");
        LocSpriteMatching.Add(WarBookLocation.BazaUni, "warLocBaseUniTerr_6");
        LocSpriteMatching.Add(WarBookLocation.BazaUSA, "warLocBaseUniUSA_6");
        LocSpriteMatching.Add(WarBookLocation.BazaDlani, "warLocBaseUniHands_6");
        LocSpriteMatching.Add(WarBookLocation.BazaTerrGory, "warLocTerrBaseDay_12-5");
        LocSpriteMatching.Add(WarBookLocation.Cityeace, "warLocCityPeace_0");
        LocSpriteMatching.Add(WarBookLocation.CityWar, "warLocCityWar_0");
        LocSpriteMatching.Add(WarBookLocation.DesertRoad, "warLocDesert_12-5");
        LocSpriteMatching.Add(WarBookLocation.MountainRoad, "warLocDesertRoad_12-5");
        LocSpriteMatching.Add(WarBookLocation.Cave, "warLocCaveLeft_0");
        LocSpriteMatching.Add(WarBookLocation.CaveSklad, "warLocCaveEnd_0");
        LocSpriteMatching.Add(WarBookLocation.Room, "warLocRoom_12-5");
        LocSpriteMatching.Add(WarBookLocation.Cabinet, "warLocCabinet_12-5");
        LocSpriteMatching.Add(WarBookLocation.Helicopter, "warLocHelicopter_12-5");
        LocSpriteMatching.Add(WarBookLocation.Bar, "warLocBar_3");
        LocSpriteMatching.Add(WarBookLocation.Dining, "warLocDining");
        LocSpriteMatching.Add(WarBookLocation.Lab, "warLocLab_8");
        LocSpriteMatching.Add(WarBookLocation.Cave2, "warLocCaveRight_0");
        LocSpriteMatching.Add(WarBookLocation.MountainRoad2, "warLocDesertRoad_12-5");
        LocSpriteMatching.Add(WarBookLocation.MountainRoadNight, "warLocDesertRoadNight_12-5");
        LocSpriteMatching.Add(WarBookLocation.MountainRoadNight2, "warLocDesertRoadNight_12-5");
        LocSpriteMatching.Add(WarBookLocation.BazaTerGoryNight, "warLocTerrBaseNight_12-5");
        LocSpriteMatching.Add(WarBookLocation.BazaTersioNight, "warLocBaseUniNightSword_6");
        LocSpriteMatching.Add(WarBookLocation.BazaDlaniNight, "warLocBaseUniNightHands_6");

        LocSoundMatching = new Dictionary<WarBookLocation, string>();

        LocSoundMatching.Add(WarBookLocation.BazaTersio, "War_base");
        LocSoundMatching.Add(WarBookLocation.BazaUni, "War_base");
        LocSoundMatching.Add(WarBookLocation.BazaUSA, "War_base");
        LocSoundMatching.Add(WarBookLocation.BazaDlani, "War_base");
        LocSoundMatching.Add(WarBookLocation.BazaTerrGory, "War_base");
        LocSoundMatching.Add(WarBookLocation.Cityeace, "War_city");
        LocSoundMatching.Add(WarBookLocation.CityWar, "War_cityWar");
        LocSoundMatching.Add(WarBookLocation.DesertRoad, "War_desert");
        LocSoundMatching.Add(WarBookLocation.MountainRoad, "War_desert");
        LocSoundMatching.Add(WarBookLocation.Cave, "War_cave");
        LocSoundMatching.Add(WarBookLocation.CaveSklad, "War_cave");
        LocSoundMatching.Add(WarBookLocation.Room, "War_office"); //
        LocSoundMatching.Add(WarBookLocation.Cabinet, "War_office");
        LocSoundMatching.Add(WarBookLocation.Helicopter, "War_helicopter");
        LocSoundMatching.Add(WarBookLocation.Bar, "War_dining"); //
        LocSoundMatching.Add(WarBookLocation.Dining, "War_dining");
        LocSoundMatching.Add(WarBookLocation.Lab, "War_office"); //
        LocSoundMatching.Add(WarBookLocation.Cave2, "War_cave");
        LocSoundMatching.Add(WarBookLocation.MountainRoad2, "War_desert");
        LocSoundMatching.Add(WarBookLocation.MountainRoadNight, "War_desert");
        LocSoundMatching.Add(WarBookLocation.MountainRoadNight2, "War_desert");
        LocSoundMatching.Add(WarBookLocation.BazaTerGoryNight, "War_base");
        LocSoundMatching.Add(WarBookLocation.BazaTersioNight, "War_base");
        LocSoundMatching.Add(WarBookLocation.BazaDlaniNight, "War_base");
    }
}

public enum FantBookCharacter
{
    Sec_Peasant1,
    Sec_Peasant2,
    Sec_Peasant3,
    Sec_Peasant4,
    Sec_Peasant5,
    Sec_Rebel1,
    Sec_Rebel2,
    Sec_Rebel3,
    Sec_Man_Robber1,
    Sec_Man_Robber2,
    Sec_Man_Robber3,
    Sec_Man_Issagur_Sergeant,
    Sec_Man_Tuan_Sergeant,
    Sec_Man_Issagur_Common1,
    Sec_Man_Issagur_Common2,
    Sec_Man_Torion_Common1,
    Sec_Man_Torion_Common2,
    Sec_Man_Tuan_Common,
    Sec_Man_Issagur_Elite,
    Sec_PleasantUlv,
    Sec_Unnamed,
    Amir,
    Bassil,
    Veronika,
    Vlas,
    Main,
    Dominic,
    Elena,
    Ifiza,
    Orin,
    Teller,
    Belinda,
    Yaromir
}

public enum FantBookLocation
{
    Bathhouse,
    CampDay,
    CampNight,
    CastleIssagur,
    CastleTorion,
    CastleTuan,
    ElenaHouse,
    FieldCastle,
    FieldCastleNight,
    FieldDay,
    FieldNight,
    ForestCampDay,
    ForestCampNight,
    ForestRoad,
    Prison,
    StreetsIssagurDay,
    StreetsIssagurNight,
    StreetsTorionDay,
    StreetsTorionNight,
    StreetsTuanDay,
    StreetsTuanNight,
    StreetsTuanWarDay,
    StreetsTuanWarNight,
    TavernEmpty,
    TavernFull,
    ThroneRoomIssagur,
    ThroneRoomTorion,
    ThroneRoomTuan
}


public static class FantasyBook
{
    public static Dictionary<string, FantBookCharacter> Characters;
    public static Dictionary<FantBookCharacter, string> ChAtalsMathcing;
    public static Dictionary<FantBookCharacter, string> ChClothesVariablesMathcing;

    public static Dictionary<string, FantBookLocation> Locations;
    public static Dictionary<FantBookLocation, string> LocSpriteMatching;
    public static Dictionary<FantBookLocation, string> LocSoundMatching;

    public static string[] Currencies = new string[] { "keys", "premium", "food", "money", "power", "army" };


    public static void Init()
    {
        Characters = new Dictionary<string, FantBookCharacter>();

        Characters.Add("Амир", FantBookCharacter.Amir);
        Characters.Add("Бассил", FantBookCharacter.Bassil);
        Characters.Add("Доминик", FantBookCharacter.Dominic);
        Characters.Add("Белинда", FantBookCharacter.Belinda);
        Characters.Add("Елена", FantBookCharacter.Elena);
        Characters.Add("Ифиза", FantBookCharacter.Ifiza);
        Characters.Add("Главный герой", FantBookCharacter.Main);
        Characters.Add("Орин", FantBookCharacter.Orin);
        Characters.Add("Крестьянин 1", FantBookCharacter.Sec_Peasant1);
        Characters.Add("Крестьянин 2", FantBookCharacter.Sec_Peasant2);
        Characters.Add("Крестьянин 3", FantBookCharacter.Sec_Peasant3);
        Characters.Add("Крестьянин 4", FantBookCharacter.Sec_Peasant4);
        Characters.Add("Мятежник 1", FantBookCharacter.Sec_Rebel1);
        Characters.Add("Мятежник 2", FantBookCharacter.Sec_Rebel2);
        Characters.Add("Мятежник 3", FantBookCharacter.Sec_Rebel3);
        Characters.Add("Разбойник 1", FantBookCharacter.Sec_Man_Robber1);
        Characters.Add("Разбойник 2", FantBookCharacter.Sec_Man_Robber2);
        Characters.Add("Разбойник 3", FantBookCharacter.Sec_Man_Robber3);
        Characters.Add("Сержант Иссагура", FantBookCharacter.Sec_Man_Issagur_Sergeant);
        Characters.Add("Сержант Туана", FantBookCharacter.Sec_Man_Tuan_Sergeant);
        Characters.Add("Солдат Архамена", FantBookCharacter.Sec_Man_Issagur_Common1);
        Characters.Add("Солдат Иссагура 1", FantBookCharacter.Sec_Man_Issagur_Common1);
        Characters.Add("Солдат Иссагура 2", FantBookCharacter.Sec_Man_Issagur_Common2);
        Characters.Add("Солдат Ториона 1", FantBookCharacter.Sec_Man_Torion_Common1);
        Characters.Add("Солдат Ториона 2", FantBookCharacter.Sec_Man_Torion_Common2);
        Characters.Add("Солдат Туана", FantBookCharacter.Sec_Man_Tuan_Common);
        Characters.Add("Стража Иссагура 1", FantBookCharacter.Sec_Man_Issagur_Elite);
        Characters.Add("Воин Безымянного братства", FantBookCharacter.Sec_Unnamed);
        Characters.Add("Ульв Свирепый", FantBookCharacter.Sec_PleasantUlv);
        Characters.Add("Элитный солдат Иссагура", FantBookCharacter.Sec_Man_Issagur_Elite);
        Characters.Add("Рассказчик", FantBookCharacter.Teller);
        Characters.Add("Вероника", FantBookCharacter.Veronika);
        Characters.Add("Влас", FantBookCharacter.Vlas);
        Characters.Add("Яромир", FantBookCharacter.Yaromir);

        Locations = new Dictionary<string, FantBookLocation>();
        Locations.Add("CampNight", FantBookLocation.CampNight);
        Locations.Add("CampDay", FantBookLocation.CampDay);
        Locations.Add("FieldCastle", FantBookLocation.FieldCastle);
        Locations.Add("FieldCastleNight", FantBookLocation.FieldCastleNight);
        Locations.Add("FieldDay", FantBookLocation.FieldDay);
        Locations.Add("FieldNight", FantBookLocation.FieldNight);
        Locations.Add("ForestCampDay", FantBookLocation.ForestCampDay);
        Locations.Add("ForestCampNight", FantBookLocation.ForestCampNight);
        Locations.Add("TavernFull", FantBookLocation.TavernFull);
        Locations.Add("TavernEmpty", FantBookLocation.TavernEmpty);
        Locations.Add("ForestRoad", FantBookLocation.ForestRoad);
        Locations.Add("CastleIssagur", FantBookLocation.CastleIssagur);
        Locations.Add("CastleTorion", FantBookLocation.CastleTorion);
        Locations.Add("CastleTuan", FantBookLocation.CastleTuan);
        Locations.Add("StreetsTuanDay", FantBookLocation.StreetsTuanDay);
        Locations.Add("StreetsIssagurDay", FantBookLocation.StreetsIssagurDay);
        Locations.Add("StreetsTorionDay", FantBookLocation.StreetsTorionDay);
        Locations.Add("StreetsTuanWarDay", FantBookLocation.StreetsTuanWarDay);
        Locations.Add("StreetsIssagurNight", FantBookLocation.StreetsIssagurNight);
        Locations.Add("StreetsTorionNight", FantBookLocation.StreetsTorionNight);
        Locations.Add("StreetsTuanNight", FantBookLocation.StreetsTuanNight);
        Locations.Add("StreetsTuanWarNight", FantBookLocation.StreetsTuanWarNight);
        Locations.Add("ThroneRoomIssagur", FantBookLocation.ThroneRoomIssagur);
        Locations.Add("ThroneRoomTorion", FantBookLocation.ThroneRoomTorion);
        Locations.Add("ThroneRoomTuan", FantBookLocation.ThroneRoomTuan);
        Locations.Add("ElenaHouse", FantBookLocation.ElenaHouse);
        Locations.Add("Bathhouse", FantBookLocation.Bathhouse);
        Locations.Add("Prison", FantBookLocation.Prison);

        ChAtalsMathcing = new Dictionary<FantBookCharacter, string>();

        ChAtalsMathcing.Add(FantBookCharacter.Amir, "Halfgods_Amir");
        ChAtalsMathcing.Add(FantBookCharacter.Bassil, "Halfgods_Bassil");
        ChAtalsMathcing.Add(FantBookCharacter.Dominic, "Halfgods_Dominic");
        ChAtalsMathcing.Add(FantBookCharacter.Ifiza, "Halfgods_Ifiza");
        ChAtalsMathcing.Add(FantBookCharacter.Belinda, "Halfgods_Belinda");
        ChAtalsMathcing.Add(FantBookCharacter.Main, "Halfgods_Main");
        ChAtalsMathcing.Add(FantBookCharacter.Orin, "Halfgods_Orin");
        ChAtalsMathcing.Add(FantBookCharacter.Veronika, "Halfgods_Veronika");
        ChAtalsMathcing.Add(FantBookCharacter.Vlas, "Halfgods_Vlas+Elena");
        ChAtalsMathcing.Add(FantBookCharacter.Elena, "Halfgods_Vlas+Elena");
        ChAtalsMathcing.Add(FantBookCharacter.Yaromir, "Halfgods_Yaromir");

        ChClothesVariablesMathcing = new Dictionary<FantBookCharacter, string>();

        ChClothesVariablesMathcing.Add(FantBookCharacter.Amir, "Amir_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Bassil, "Bassil_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Dominic, "Dominic_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Elena, "Elena_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Ifiza, "Ifiza_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Main, "Main_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Orin, "Orin_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Veronika, "Veronika_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Vlas, "Vlas_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Yaromir, "Yaromir_Dress");
        ChClothesVariablesMathcing.Add(FantBookCharacter.Belinda, "Belinda_Dress");

        LocSpriteMatching = new Dictionary<FantBookLocation, string>();

        LocSpriteMatching.Add(FantBookLocation.CampNight, "HalfgodsLocCampnight_15(4C18)");
        LocSpriteMatching.Add(FantBookLocation.CampDay, "HalfgodsLocCamp_15(4C15)");
        LocSpriteMatching.Add(FantBookLocation.FieldCastle, "HalfgodsLocGate_0(55E4)");
        LocSpriteMatching.Add(FantBookLocation.FieldCastleNight, "HalfgodsLocGateNight_0(55E7)");
        LocSpriteMatching.Add(FantBookLocation.FieldDay, "HalfgodsLocRoad_15(55EA)");
        LocSpriteMatching.Add(FantBookLocation.FieldNight, "HalfgodsLocRoadNight_15(55ED)");
        LocSpriteMatching.Add(FantBookLocation.ForestCampDay, "halfgodsLocForestCampDay(4C24)");
        LocSpriteMatching.Add(FantBookLocation.ForestCampNight, "halfgodsLocForestCampNight(4C27)");
        LocSpriteMatching.Add(FantBookLocation.TavernFull, "HalfgodsLocTavern_12-5(55F0)");
        LocSpriteMatching.Add(FantBookLocation.TavernEmpty, "HalfgodsLocTavernEmpty_12-5(55F3)");
        LocSpriteMatching.Add(FantBookLocation.ForestRoad, "HalfgoodsLocForestroad_0(540B)");
        LocSpriteMatching.Add(FantBookLocation.CastleIssagur, "CastleIssagur");
        LocSpriteMatching.Add(FantBookLocation.CastleTorion, "CastleTorion");
        LocSpriteMatching.Add(FantBookLocation.CastleTuan, "CastleTuan");
        LocSpriteMatching.Add(FantBookLocation.StreetsTuanDay, "HalfgodsLocTownTuanDay_12-5(5405)");
        LocSpriteMatching.Add(FantBookLocation.StreetsIssagurDay, "HalfgodsLocTownArhamenDay_12-5(55F6)");
        LocSpriteMatching.Add(FantBookLocation.StreetsTorionDay, "HalfgodsLocTownTorionDay_12-5(55FF)");
        LocSpriteMatching.Add(FantBookLocation.StreetsTuanWarDay, "HalfgodsLocTownFire_12-5(55FC)");
        LocSpriteMatching.Add(FantBookLocation.StreetsIssagurNight, "HalfgodsLocTownArhamenNight_12-5(55F9)");
        LocSpriteMatching.Add(FantBookLocation.StreetsTorionNight, "HalfgodsLocTownTorionNight_12-5(5402)");
        LocSpriteMatching.Add(FantBookLocation.StreetsTuanNight, "HalfgodsLocTownTuanNight_12-5(5408)");
        LocSpriteMatching.Add(FantBookLocation.StreetsTuanWarNight, "HalfgodsLocTownTuanNightFire_12-5");
        LocSpriteMatching.Add(FantBookLocation.ThroneRoomIssagur, "halfgodsLocThroneRoomIssagur_8-7(4C2A)");
        LocSpriteMatching.Add(FantBookLocation.ThroneRoomTorion, "halfgodsLocThroneRoomTorion_8-7(4C2D)");
        LocSpriteMatching.Add(FantBookLocation.ThroneRoomTuan, "halfgodsLocThroneRoomTuan_8-7(4C12)");
        LocSpriteMatching.Add(FantBookLocation.ElenaHouse, "HalfgodsLocHelensHut_8(55DB)");
        LocSpriteMatching.Add(FantBookLocation.Bathhouse, "HalfgodsLocBath_8(5417)");
        LocSpriteMatching.Add(FantBookLocation.Prison, "HalfgodsLocMedievalPrison_12-5(55D8)");

        LocSoundMatching = new Dictionary<FantBookLocation, string>();


        LocSoundMatching.Add(FantBookLocation.CampNight, "Halfgods_camp");
        LocSoundMatching.Add(FantBookLocation.CampDay, "Halfgods_camp");
        LocSoundMatching.Add(FantBookLocation.FieldCastle, "Halfgods_field");
        LocSoundMatching.Add(FantBookLocation.FieldCastleNight, "Halfgods_field");
        LocSoundMatching.Add(FantBookLocation.FieldDay, "Halfgods_field");
        LocSoundMatching.Add(FantBookLocation.FieldNight, "Halfgods_field");
        LocSoundMatching.Add(FantBookLocation.ForestCampDay, "Halfgods_forrestCamp");
        LocSoundMatching.Add(FantBookLocation.ForestCampNight, "Halfgods_campCelebration");
        LocSoundMatching.Add(FantBookLocation.TavernFull, "Halfgods_tawern");
        LocSoundMatching.Add(FantBookLocation.TavernEmpty, "Halfgods_empty");
        LocSoundMatching.Add(FantBookLocation.ForestRoad, "Halfgods_forrest");
        LocSoundMatching.Add(FantBookLocation.CastleIssagur, "Halfgods_castle");
        LocSoundMatching.Add(FantBookLocation.CastleTorion, "Halfgods_castle");
        LocSoundMatching.Add(FantBookLocation.CastleTuan, "Halfgods_castle");
        LocSoundMatching.Add(FantBookLocation.StreetsTuanDay, "Halfgods_town");
        LocSoundMatching.Add(FantBookLocation.StreetsIssagurDay, "Halfgods_town");
        LocSoundMatching.Add(FantBookLocation.StreetsTorionDay, "Halfgods_town");
        LocSoundMatching.Add(FantBookLocation.StreetsTuanWarDay, "Halfgods_townWar");
        LocSoundMatching.Add(FantBookLocation.StreetsIssagurNight, "Halfgods_town");
        LocSoundMatching.Add(FantBookLocation.StreetsTorionNight, "Halfgods_town");
        LocSoundMatching.Add(FantBookLocation.StreetsTuanNight, "Halfgods_town");
        LocSoundMatching.Add(FantBookLocation.StreetsTuanWarNight, "Halfgods_townWar");
        LocSoundMatching.Add(FantBookLocation.ThroneRoomIssagur, "Halfgods_castle");
        LocSoundMatching.Add(FantBookLocation.ThroneRoomTorion, "Halfgods_castle");
        LocSoundMatching.Add(FantBookLocation.ThroneRoomTuan, "Halfgods_castle");
        LocSoundMatching.Add(FantBookLocation.ElenaHouse, "Halfgods_empty");
        LocSoundMatching.Add(FantBookLocation.Bathhouse, "Halfgods_empty");
        LocSoundMatching.Add(FantBookLocation.Prison, "Halfgods_empty");
    }
}


public enum EHeavenBookCharacter
{
    Akemi,
    Amelie,
    Main,
    Johnny,
    Conor,
    Kori,
    Leon,
    Masha,
    Mira,
    Teller
}

public enum EHeavenBookLocation
{
    Bunker,
    BunkerInside,
    CampHouse,
    Hearth,
    Camp,
    Lake,
    LakeNight,
    Cave,
    Beach,
    Radio,
    JungleBase,
    JungleNight,
    JungleCave,
    JungleDay,
    JunglePath,
    BlackScreen
}

public static class HeavenBook
{
    public static Dictionary<string, EHeavenBookCharacter> Characters;
    public static Dictionary<EHeavenBookCharacter, string> ChAtalsMathcing;
    public static Dictionary<EHeavenBookCharacter, string> ChClothesVariablesMathcing;

    public static Dictionary<string, EHeavenBookLocation> Locations;
    public static Dictionary<EHeavenBookLocation, string> LocSpriteMatching;
    public static Dictionary<EHeavenBookLocation, string> LocSoundMatching;

    public static string[] Currencies = new string[] { "keys", "premium", "fear", "friendship" };

    public static void Init()
    {
        Characters = new Dictionary<string, EHeavenBookCharacter>();

        Characters.Add("Акеми", EHeavenBookCharacter.Akemi);
        Characters.Add("Амели", EHeavenBookCharacter.Amelie);
        Characters.Add("Главный Герой", EHeavenBookCharacter.Main);
        Characters.Add("Джонни", EHeavenBookCharacter.Johnny);
        Characters.Add("Конор", EHeavenBookCharacter.Conor);
        Characters.Add("Кори", EHeavenBookCharacter.Kori);
        Characters.Add("Леон", EHeavenBookCharacter.Leon);
        Characters.Add("Маша", EHeavenBookCharacter.Masha);
        Characters.Add("Мира", EHeavenBookCharacter.Mira);
        Characters.Add("Рассказчик", EHeavenBookCharacter.Teller);

        Locations = new Dictionary<string, EHeavenBookLocation>();

        Locations.Add("Бункер", EHeavenBookLocation.Bunker);
        Locations.Add("Бункер. Внутри", EHeavenBookLocation.BunkerInside);
        Locations.Add("Дом в лагере выживших в авиакатастрофе", EHeavenBookLocation.CampHouse);
        Locations.Add("Костер", EHeavenBookLocation.Hearth);
        Locations.Add("Лагерь выживших в авиакатастрофе", EHeavenBookLocation.Camp);
        Locations.Add("Озеро", EHeavenBookLocation.Lake);
        Locations.Add("Озеро. Ночь", EHeavenBookLocation.LakeNight);
        Locations.Add("Пещера каннибалов", EHeavenBookLocation.Cave);
        Locations.Add("Пляж", EHeavenBookLocation.Beach);
        Locations.Add("Радиовышка", EHeavenBookLocation.Radio);
        Locations.Add("Тропический лес. Базовый", EHeavenBookLocation.JungleBase);
        Locations.Add("Тропический лес. Ночь", EHeavenBookLocation.JungleNight);
        Locations.Add("Тропический лес. Пещера", EHeavenBookLocation.JungleCave);
        Locations.Add("Тропический лес. Светлый", EHeavenBookLocation.JungleDay);
        Locations.Add("Тропический лес. Тропа", EHeavenBookLocation.JunglePath);
        Locations.Add("Черный экран", EHeavenBookLocation.BlackScreen);

        ChAtalsMathcing = new Dictionary<EHeavenBookCharacter, string>();

        ChAtalsMathcing.Add(EHeavenBookCharacter.Akemi, "akemie_amelie");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Amelie, "akemie_amelie");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Conor, "conor_kori");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Johnny, "johny_leon");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Kori, "conor_kori");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Leon, "johny_leon");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Main, "Paradise_Main");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Masha, "Paradise_Masha+Mira");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Mira, "Paradise_Masha+Mira");
        ChAtalsMathcing.Add(EHeavenBookCharacter.Teller, "-");

        ChClothesVariablesMathcing = new Dictionary<EHeavenBookCharacter, string>();

        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Akemi, "Akemy_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Amelie, "Amely_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Conor, "Konor_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Johnny, "Jonny_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Kori, "Kori_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Leon, "Leon_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Main, "MainHero_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Masha, "Masha_Dress");
        ChClothesVariablesMathcing.Add(EHeavenBookCharacter.Mira, "Mira_Dress");

        LocSpriteMatching = new Dictionary<EHeavenBookLocation, string>();

        LocSpriteMatching.Add(EHeavenBookLocation.Beach, "heavenLocJungleBeach_12-5");
        LocSpriteMatching.Add(EHeavenBookLocation.BlackScreen, "BlackScreen");
        LocSpriteMatching.Add(EHeavenBookLocation.Bunker, "heavenLocBunker_6-3");
        LocSpriteMatching.Add(EHeavenBookLocation.BunkerInside, "heavenLocBunkerInside_12-5");
        LocSpriteMatching.Add(EHeavenBookLocation.Camp, "heavenLocPlaneCrashCamp_9-7");
        LocSpriteMatching.Add(EHeavenBookLocation.CampHouse, "heavenLocPlaneCrashHouse_12-5");
        LocSpriteMatching.Add(EHeavenBookLocation.Cave, "heavenLocCannibalCave_25-0");
        LocSpriteMatching.Add(EHeavenBookLocation.Hearth, "heavenLocCampfire_15-6");
        LocSpriteMatching.Add(EHeavenBookLocation.JungleBase, "heavenLocDenseJungle_11-0");
        LocSpriteMatching.Add(EHeavenBookLocation.JungleCave, "heavenLocPathCave_16-2");
        LocSpriteMatching.Add(EHeavenBookLocation.JungleDay, "heavenLocSparseJungle_12-5");
        LocSpriteMatching.Add(EHeavenBookLocation.JungleNight, "heavenLocNightJungle_12-5");
        LocSpriteMatching.Add(EHeavenBookLocation.JunglePath, "heavenLocPathJungle_16-2");
        LocSpriteMatching.Add(EHeavenBookLocation.Lake, "heavenLocLake_6-3");
        LocSpriteMatching.Add(EHeavenBookLocation.LakeNight, "heavenLocNightLake_6-3");
        LocSpriteMatching.Add(EHeavenBookLocation.Radio, "heavenLocRadioTower_3-0");

        LocSoundMatching = new Dictionary<EHeavenBookLocation, string>();

        LocSoundMatching.Add(EHeavenBookLocation.Beach, "Heaven_beach");
        LocSoundMatching.Add(EHeavenBookLocation.BlackScreen, "");
        LocSoundMatching.Add(EHeavenBookLocation.Bunker, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.BunkerInside, "Heaven_inside");
        LocSoundMatching.Add(EHeavenBookLocation.Camp, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.CampHouse, "Heaven_inside");
        LocSoundMatching.Add(EHeavenBookLocation.Cave, "Heaven_cave");
        LocSoundMatching.Add(EHeavenBookLocation.Hearth, "Heaven_bonfire");
        LocSoundMatching.Add(EHeavenBookLocation.JungleBase, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.JungleCave, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.JungleDay, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.JungleNight, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.JunglePath, "Heaven_forrest");
        LocSoundMatching.Add(EHeavenBookLocation.Lake, "Heaven_waterfall");
        LocSoundMatching.Add(EHeavenBookLocation.LakeNight, "Heaven_waterfall");
        LocSoundMatching.Add(EHeavenBookLocation.Radio, "Heaven_forrest"); //
    }
}