// =====================================================================================
//  LPC TERRAINS -> UNITY  (script de Editor)
// =====================================================================================
//  Menú:  Tools > LPC Terrains > Generar Rule Tiles
//
//  Qué hace:
//   1. Configura la textura "terrain-v7-extruded.png" para pixel art (32 px por unidad,
//      filtro Point, sin compresión) y la corta en tiles de 32x32.
//   2. Crea un Rule Tile por cada terreno (LPC_Grass, LPC_Water, LPC_Sand...) en la
//      carpeta "Tiles" junto a la textura. Cada Rule Tile elige solo el borde/esquina
//      correcto según sus vecinos, de esta forma queda un terreno coherente
//
//  Se puede ejecutar varias veces: reutiliza los sprites y los tiles existentes, por lo
//  que lo que ya hayas pintado en tus escenas no se pierde.
//
//  Este archivo para su funcionamiento debe estar dentro de una carpeta llamada "Editor".
//
//  Arte: "[LPC] Terrains" por bluecarrot16 y otros (CC-BY-SA 3.0 / GPL 3.0).
// =====================================================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class LPCTerrainImporter
{
    private const string TextureName = "terrain-v7-extruded";
    private const int TileSize = 32;              // píxeles de cada tile en el arte original
    private const int CellSize = TileSize + 2;    // 1 px de extrusión por lado (evita líneas entre tiles)
    private const int Columns = 32;
    private const int Rows = 64;
    private const int PixelsPerUnit = 32;         // 1 tile = 1 unidad = 1 celda del Grid
    private const float RandomScale = 0.83f;      // variedad de los tiles de relleno

    // Valores de vecino que usa RuleTile (RuleTile.TilingRuleOutput.Neighbor)
    private const int DontCare = 0;
    private const int This = 1;
    private const int NotThis = 2;

    // Orden estándar de los 8 vecinos de un Rule Tile
    private static readonly Vector3Int[] NeighborPositions =
    {
        new Vector3Int(-1, 1, 0),  // 0 NO
        new Vector3Int(0, 1, 0),   // 1 N
        new Vector3Int(1, 1, 0),   // 2 NE
        new Vector3Int(-1, 0, 0),  // 3 O
        new Vector3Int(1, 0, 0),   // 4 E
        new Vector3Int(-1, -1, 0), // 5 SO
        new Vector3Int(0, -1, 0),  // 6 S
        new Vector3Int(1, -1, 0),  // 7 SE
    };

    // Formato: Nombre | tile principal | tiene bordes (1/0) | 16 listas de tiles separadas por ';'
    // La lista k corresponde a la máscara de esquinas k (bit0 = arriba-izq, bit1 = arriba-der,
    // bit2 = abajo-izq, bit3 = abajo-der). Extraído de terrain-v7.tsx (formato Tiled).
    private static readonly string[] TerrainData =
    {
        "Dirt_Tan|97|1|32;130;128;129;66;98;192;1;64;193;96;2;65;33;34;97 97 97 160 161 162",
        "Dirt_Brown|100|1|35;133;131;132;69;101;195;4;67;196;99;5;68;36;37;100 100 100 163 164 165",
        "Dirt_Dark|103|1|38;136;134;135;72;104;198;7;70;199;102;8;71;39;40;103 103 103 166 167 168",
        "Rock_White|106|1|41;139;137;138;75;107;201;10;73;202;105;11;74;42;43;106 106 106 169 170 171",
        "Rock_Gray|109|1|44;142;140;141;78;110;204;13;76;205;108;14;77;45;46;109 109 109 172 173 174",
        "Rock_Dark|112|1|47;145;143;144;81 209;113;207;16;79;208;111;17;80;48;49;112 112 112 175 176 177",
        "Rock_Black|115|1|50;148;146;147;84;116;210;19;82;211;114;20;83;51;52;115 115 115 178 179 180",
        "Hole_Brown|118|1|53;151;149;150;87;119;213;22;85;214;117;23;86;54;55;118 118 118",
        "Hole_Black|121|1|56;154;152;153;90;122;216;25;88;217;120;26;89;57;58;121 121 121",
        "Mud_Brown|124|1|;157;155;156;93;125;219;28;91;220;123;29;92;60;61;124 124 124 187 188 189",
        "Grass|321|1|256;354;352;353;290;322;416;225;288;417;320;226;289;257;258;321 321 321 384 385 386 418",
        "Grass_Light|324|1|259;357;355;356;293;325;419;228;291;420;323;229;292;260;261;324 324 324 387 388 389 421",
        "Grass_Dark|327|1|262;360;358;359;296;328;422;231;294;423;326;232;295;263;264;327 327 327 390 391 392",
        "Grass_Dead|330|1|265;363;361;362;299;331;425;234;297;426;329;235;298;266;267;330 330 330 393 394 395 427",
        "Soil|333|1|;366;364;365;302;334;428;237;300;429;332;238;301;269;270;333 333 333 1130",
        "Sand|336|1|271;369;367;368;305;337;431;240;303;432;335;241;304;272;273;336 336 336 399 400 401 769 772",
        "Snow_1|339|1|274;372;370;371;308;340;434;243;306;435;338;244;307;275;276;339 339 339 402 403 404 775 778 781",
        "Snow_2|342|1|277;375;373;374;311;343;437;246;309;438;341;247;310;278;279;342 342 342 405 406 407",
        "Gravel_1|345|1|280;378;376;377;314;346;440;249;312;441;344;250;313;281;282;345 345 345 408 409",
        "Dirt_Roots|348|1|283;381;379;380;317;349;443;252;315;444;347;253;316;284;285;348 348 348 411 412 1369 1372",
        "Water_Shallows_Dirt|545|1|480;578;576;577;514;546;640;449;512;641;544;450;513;481;482;545 545 545 608 609 610 663 1363 1366",
        "Water|548|1|483;581;579;580;517;549;643;452;515;644;547;453;516;484;485;548 548 548 566 569 572 611 612 613",
        "Water_Deep|551|1|486;584;582;583;520;552;646;455;518;647;550;456;519;487;488;551 551 551 563 614 615 616 626 627 628",
        "Water_Purple|554|1|489;587;585;586;523;555;649;458;521;650;553;459;522;490;491;554 554 554 617 618 619",
        "Water_Green|557|1|492;590;588;589;526;558;652;461;524;653;556;462;525;493;494;557 557 557 620 621 622",
        "Lava|560|1|495;593;591;592;529;561;655;464;527;656;559;465;528;496;497;560 560 560 623 624 625",
        "Water_Shallows_Sand|837|0|837 837 837 867 868",
        "Ice|838|0|838 838 838 839 840",
        "Ice_Melting|841|0|841 841 841 681 713 842 843",
        "Earth_Cracked|784|1|719;817;815;816;753;785;879;688;751;880;783;689;752;720;721;784 784 784 847 848 849",
        "Stone_White|787|1|;820;818;819;756;788;882;691;754;883;786;692;755;723;724;787 787 787 850 851 852 1348",
        "Stone_Tan|790|1|;823;821;822;759;791;885;694;757;886;789;695;758;726;727;790 790 790 853 854 855 1351",
        "Mudstone_Gray|793|1|;826;824;825;762;794;888;697;760 889;;792;698;761;729;730;793 793 793 856 857 858 1354",
        "Mudstone_Brown|796|1|;829;827;828;765;797;891;700;763;892;795;701;764;732;733;796 796 796 859 860 861 1360",    };

    private class TerrainInfo
    {
        public string name;
        public int mainTile;
        public bool hasEdges;
        public int[][] masks; // 16 entradas (o solo la 15 si no tiene bordes)
    }

    [MenuItem("Tools/LPC Terrains/Generar Rule Tiles")]
    public static void GenerateAll()
    {
        string texturePath = FindTexturePath();
        if (texturePath == null)
        {
            EditorUtility.DisplayDialog("LPC Terrains",
                $"No encuentro '{TextureName}.png' dentro de Assets.\nImpórtala primero (paso 2 de la guía).", "Vale");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("LPC Terrains", "Configurando y cortando la textura...", 0.2f);
            SliceTexture(texturePath);

            EditorUtility.DisplayProgressBar("LPC Terrains", "Creando Rule Tiles...", 0.7f);
            int created = CreateRuleTiles(texturePath, out string tilesFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(tilesFolder);
            EditorGUIUtility.PingObject(folder);
            EditorUtility.DisplayDialog("LPC Terrains",
                $"¡Listo! Se han generado {created} Rule Tiles en:\n{tilesFolder}", "Genial");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static string FindTexturePath()
    {
        foreach (string guid in AssetDatabase.FindAssets(TextureName + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == TextureName) return path;
        }
        return null;
    }

    // ---------------------------------------------------------------- textura ----------

    private static void SliceTexture(string texturePath)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

        // Ajustes de pixel art
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096; // la textura mide 1088x2176: con el máximo por defecto (2048) se vería borrosa

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // recomendado para tiles
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        // Leemos los píxeles desde el PNG para saltarnos las celdas vacías
        var pixelsTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        pixelsTex.LoadImage(File.ReadAllBytes(Path.GetFullPath(texturePath)));
        Color32[] pixels = pixelsTex.GetPixels32();
        int width = pixelsTex.width;
        int height = pixelsTex.height;
        Object.DestroyImmediate(pixelsTex);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // Conservamos los IDs de sprites ya existentes para no romper referencias al regenerar
        var existingIds = new Dictionary<string, GUID>();
        foreach (SpriteRect r in provider.GetSpriteRects())
            existingIds[r.name] = r.spriteID;

        var rects = new List<SpriteRect>();
        var pairs = new List<SpriteNameFileIdPair>();

        for (int id = 0; id < Columns * Rows; id++)
        {
            int col = id % Columns;
            int row = id / Columns;
            int x = col * CellSize + 1;
            int y = height - (row * CellSize + 1) - TileSize; // Unity cuenta Y desde abajo

            if (IsEmpty(pixels, width, x, y)) continue;

            string spriteName = SpriteName(id);
            GUID spriteId = existingIds.TryGetValue(spriteName, out GUID old) ? old : GUID.Generate();

            rects.Add(new SpriteRect
            {
                name = spriteName,
                rect = new Rect(x, y, TileSize, TileSize),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = spriteId
            });
            pairs.Add(new SpriteNameFileIdPair(spriteName, spriteId));
        }

        provider.SetSpriteRects(rects.ToArray());
        var nameFileIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIdProvider?.SetNameFileIdPairs(pairs);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static bool IsEmpty(Color32[] pixels, int width, int x0, int y0)
    {
        for (int y = y0; y < y0 + TileSize; y++)
            for (int x = x0; x < x0 + TileSize; x++)
                if (pixels[y * width + x].a > 0) return false;
        return true;
    }

    private static string SpriteName(int id) => "lpc_" + id;

    // ------------------------------------------------------------- rule tiles ----------

    private static int CreateRuleTiles(string texturePath, out string tilesFolder)
    {
        string baseFolder = Path.GetDirectoryName(texturePath).Replace('\\', '/');
        tilesFolder = baseFolder + "/Tiles";
        if (!AssetDatabase.IsValidFolder(tilesFolder))
            AssetDatabase.CreateFolder(baseFolder, "Tiles");

        var sprites = new Dictionary<string, Sprite>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>())
            sprites[s.name] = s;

        int count = 0;
        foreach (TerrainInfo info in ParseData())
        {
            Sprite main = GetSprite(sprites, info.mainTile);
            if (main == null)
            {
                Debug.LogWarning($"LPC Terrains: falta el sprite principal de {info.name}, se omite.");
                continue;
            }

            string assetName = info.hasEdges ? $"LPC_{info.name}" : $"LPC_{info.name}_SinBordes";
            string assetPath = $"{tilesFolder}/{assetName}.asset";

            RuleTile tile = AssetDatabase.LoadAssetAtPath<RuleTile>(assetPath);
            bool isNew = tile == null;
            if (isNew) tile = ScriptableObject.CreateInstance<RuleTile>();

            tile.m_DefaultSprite = main; // es la miniatura que verás en el Project
            tile.m_DefaultColliderType = Tile.ColliderType.Sprite;
            tile.m_TilingRules = info.hasEdges ? BuildEdgeRules(info, sprites) : BuildFillOnlyRules(info, sprites);

            if (isNew) AssetDatabase.CreateAsset(tile, assetPath);
            else EditorUtility.SetDirty(tile);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Las 47 combinaciones posibles de vecinos. Una esquina de la celda lleva terreno
    /// solo si las 3 celdas que la rodean (p. ej. N, O y NO) también lo tienen.
    /// </summary>
    private static List<RuleTile.TilingRule> BuildEdgeRules(TerrainInfo info, Dictionary<string, Sprite> sprites)
    {
        var rules = new List<RuleTile.TilingRule>();

        for (int cardinals = 0; cardinals < 16; cardinals++)
        {
            bool n = (cardinals & 1) != 0;
            bool e = (cardinals & 2) != 0;
            bool s = (cardinals & 4) != 0;
            bool w = (cardinals & 8) != 0;

            // Diagonales que importan: solo si sus dos vecinos rectos tienen terreno
            var relevant = new List<int>();
            if (n && w) relevant.Add(0); // NO
            if (n && e) relevant.Add(2); // NE
            if (s && w) relevant.Add(5); // SO
            if (s && e) relevant.Add(7); // SE

            for (int combo = 0; combo < (1 << relevant.Count); combo++)
            {
                int[] neighbors = new int[8];
                neighbors[1] = n ? This : NotThis;
                neighbors[4] = e ? This : NotThis;
                neighbors[6] = s ? This : NotThis;
                neighbors[3] = w ? This : NotThis;

                var filledDiagonals = new HashSet<int>();
                for (int i = 0; i < relevant.Count; i++)
                {
                    bool filled = (combo & (1 << i)) != 0;
                    neighbors[relevant[i]] = filled ? This : NotThis;
                    if (filled) filledDiagonals.Add(relevant[i]);
                }

                int mask = 0;
                if (filledDiagonals.Contains(0)) mask |= 1; // arriba-izquierda
                if (filledDiagonals.Contains(2)) mask |= 2; // arriba-derecha
                if (filledDiagonals.Contains(5)) mask |= 4; // abajo-izquierda
                if (filledDiagonals.Contains(7)) mask |= 8; // abajo-derecha

                rules.Add(MakeRule(neighbors, ResolveSprites(info, mask, sprites)));
            }
        }
        return rules;
    }

    private static List<RuleTile.TilingRule> BuildFillOnlyRules(TerrainInfo info, Dictionary<string, Sprite> sprites)
    {
        return new List<RuleTile.TilingRule> { MakeRule(new int[8], ResolveSprites(info, 15, sprites)) };
    }

    private static RuleTile.TilingRule MakeRule(int[] neighbors, Sprite[] output)
    {
        var rule = new RuleTile.TilingRule
        {
            m_Neighbors = new List<int>(neighbors),
            m_NeighborPositions = new List<Vector3Int>(NeighborPositions),
            m_Sprites = output,
            m_Output = output.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random
                                         : RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_PerlinScale = RandomScale,
            m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
            m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
            m_ColliderType = Tile.ColliderType.Sprite
        };
        return rule;
    }

    private static Sprite[] ResolveSprites(TerrainInfo info, int mask, Dictionary<string, Sprite> sprites)
    {
        var result = new List<Sprite>();
        int[] ids = mask < info.masks.Length ? info.masks[mask] : null;
        if (ids != null)
            foreach (int id in ids)
            {
                Sprite s = GetSprite(sprites, id);
                if (s != null) result.Add(s);
            }

        // Si el tileset no trae esa combinación, usamos el relleno principal
        if (result.Count == 0) result.Add(GetSprite(sprites, info.mainTile));
        return result.ToArray();
    }

    private static Sprite GetSprite(Dictionary<string, Sprite> sprites, int id)
    {
        return sprites.TryGetValue(SpriteName(id), out Sprite s) ? s : null;
    }

    private static IEnumerable<TerrainInfo> ParseData()
    {
        foreach (string line in TerrainData)
        {
            string[] parts = line.Split('|');
            bool hasEdges = parts[2] == "1";
            string[] lists = parts[3].Split(';');

            var info = new TerrainInfo
            {
                name = parts[0],
                mainTile = int.Parse(parts[1]),
                hasEdges = hasEdges,
                masks = new int[16][]
            };

            for (int i = 0; i < lists.Length; i++)
            {
                int mask = hasEdges ? i : 15;
                info.masks[mask] = lists[i]
                    .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray();
            }
            yield return info;
        }
    }
}
#endif
