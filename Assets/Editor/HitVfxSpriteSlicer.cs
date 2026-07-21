using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeskSlayer.EditorTools
{
    /// <summary>
    /// 命中特效 Sprite Sheet 程式化切割工具。掃描指定資料夾底下的所有貼圖，
    /// 依內容自動偵測透明分隔線推算網格列數/欄數，避免對排版格式做固定假設；
    /// 若素材本身格子間沒有透明留白導致自動偵測失敗，可傳入 overrideColumns/overrideRows 手動指定。
    /// 每次執行都會在 Console 印出每個檔案切出的 Sprite 數量與各自的 Rect，供切割後的人工格線確認，
    /// 避免格線判斷錯誤時只在播放動畫時才用跳幀/裁切錯位的方式顯性出現。
    /// </summary>
    public static class HitVfxSpriteSlicer
    {
        private const float AlphaThreshold = 0.02f;

        [MenuItem("DeskSlayer/VFX/Slice Hit VFX Sprite Sheets (Auto)")]
        private static void SliceKnownFoldersMenuItem()
        {
            SliceKnownFolders();
        }

        /// <summary>切割三個既定資料夾（LightAttack/HeavyAttack/EnemyHit）底下所有貼圖，各自依內容獨立偵測網格。</summary>
        public static void SliceKnownFolders()
        {
            string[] folders =
            {
                "Assets/Art/VFX/LightAttack",
                "Assets/Art/VFX/HeavyAttack",
                "Assets/Art/VFX/EnemyHit",
            };

            foreach (string folder in folders)
            {
                SliceFolder(folder);
            }
        }

        /// <summary>
        /// 切割指定資料夾底下所有貼圖，各自獨立偵測網格並套用切割，回傳每個檔案的切割結果供上層檢查。
        /// overrideColumns/overrideRows 皆大於 0 時，改用「圖片尺寸 / 欄列數」的等分網格取代內容偵測，
        /// 用於自動偵測失效（例如格子間無透明留白）的素材。
        /// </summary>
        public static List<SliceReport> SliceFolder(string folderPath, int overrideColumns = 0, int overrideRows = 0)
        {
            var reports = new List<SliceReport>();
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"[HitVfxSpriteSlicer] 資料夾不存在，略過：{folderPath}");
                return reports;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SliceReport report = SliceTexture(assetPath, overrideColumns, overrideRows);
                if (report != null)
                {
                    reports.Add(report);
                }
            }

            LogReports(folderPath, reports);
            return reports;
        }

        /// <summary>切割單一貼圖並套用到其 TextureImporter.spritesheet，回傳切割結果。</summary>
        public static SliceReport SliceTexture(string assetPath, int overrideColumns = 0, int overrideRows = 0)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[HitVfxSpriteSlicer] 找不到 TextureImporter：{assetPath}");
                return null;
            }

            bool originalReadable = importer.isReadable;

            // 內容偵測需要在 CPU 端讀取像素，暫時開啟 isReadable，切割完成後還原，
            // 避免素材長期保留一份 CPU 端副本，不必要地增加常駐記憶體佔用。
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;

            // 比照專案內 Character/Enemy 素材統一採用 48 Pixels Per Unit（見 Punk/Boss 系列貼圖的
            // TextureImporter 設定），避免沿用 Unity 預設值 100 導致命中特效在畫面上顯得比角色/敵人小上一截。
            importer.spritePixelsPerUnit = 48f;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                Debug.LogError($"[HitVfxSpriteSlicer] 無法載入 Texture2D：{assetPath}");
                importer.isReadable = originalReadable;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                return null;
            }

            List<RectInt> cellRects;
            try
            {
                cellRects = (overrideColumns > 0 && overrideRows > 0)
                    ? BuildUniformGrid(texture.width, texture.height, overrideColumns, overrideRows)
                    : DetectGridByContent(texture);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"[HitVfxSpriteSlicer] {assetPath} 自動偵測網格失敗：{exception.Message}，" +
                    "請改用 overrideColumns/overrideRows 手動指定欄列數後重新執行");
                importer.isReadable = originalReadable;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                return null;
            }

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            var spriteMetaData = new List<SpriteMetaData>(cellRects.Count);
            for (int i = 0; i < cellRects.Count; i++)
            {
                RectInt r = cellRects[i];
                spriteMetaData.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{i}",
                    rect = new Rect(r.x, r.y, r.width, r.height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
            }

            // TextureImporter.spritesheet 是官方文件仍在使用、目前唯一能純程式化切割 Sprite 格線的公開 API，
            // 雖標示為 Obsolete（官方建議改用 UnityEditor.U2D.Sprites 的 DataProvider API），
            // 但該替代 API 需另外引用 2D Sprite 編輯器組件，此處維持沿用文件仍支援的既有寫法。
#pragma warning disable CS0618
            importer.spritesheet = spriteMetaData.ToArray();
#pragma warning restore CS0618

            importer.isReadable = originalReadable;
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            return new SliceReport
            {
                AssetPath = assetPath,
                ImageWidth = texture.width,
                ImageHeight = texture.height,
                SpriteCount = cellRects.Count,
                Rects = cellRects,
            };
        }

        /// <summary>
        /// 依內容偵測格線：掃描整張圖找出「完全透明」的列/欄作為分隔線，
        /// 分隔線之間的連續非透明區塊視為一個網格 band，交叉列 band 與欄 band 得到每一格的 Rect。
        /// 讀取順序依 Texture2D 座標（原點在左下角）換算成「由上到下、由左到右」的動畫幀閱讀順序。
        /// </summary>
        private static List<RectInt> DetectGridByContent(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            byte alphaThresholdByte = (byte)Mathf.RoundToInt(AlphaThreshold * 255f);

            bool[] columnHasContent = new bool[width];
            bool[] rowHasContent = new bool[height];

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[rowOffset + x].a > alphaThresholdByte)
                    {
                        columnHasContent[x] = true;
                        rowHasContent[y] = true;
                    }
                }
            }

            List<(int start, int end)> columnBands = FindBands(columnHasContent);
            List<(int start, int end)> rowBands = FindBands(rowHasContent);

            if (columnBands.Count == 0 || rowBands.Count == 0)
            {
                throw new InvalidOperationException("偵測不到任何非透明內容");
            }

            var orderedRowBands = rowBands.OrderByDescending(b => b.start).ToList();
            var orderedColumnBands = columnBands.OrderBy(b => b.start).ToList();

            var rects = new List<RectInt>();
            foreach ((int start, int end) rowBand in orderedRowBands)
            {
                foreach ((int start, int end) colBand in orderedColumnBands)
                {
                    int w = colBand.end - colBand.start + 1;
                    int h = rowBand.end - rowBand.start + 1;
                    rects.Add(new RectInt(colBand.start, rowBand.start, w, h));
                }
            }

            return rects;
        }

        /// <summary>找出布林陣列中連續 true 的區間（band），區間之間以連續 false 分隔。</summary>
        private static List<(int start, int end)> FindBands(bool[] hasContent)
        {
            var bands = new List<(int start, int end)>();
            int start = -1;
            for (int i = 0; i < hasContent.Length; i++)
            {
                if (hasContent[i] && start == -1)
                {
                    start = i;
                }
                else if (!hasContent[i] && start != -1)
                {
                    bands.Add((start, i - 1));
                    start = -1;
                }
            }

            if (start != -1)
            {
                bands.Add((start, hasContent.Length - 1));
            }

            return bands;
        }

        /// <summary>依「圖片尺寸 / 欄列數」等分網格，不檢視內容，供內容偵測失效時手動覆寫使用。</summary>
        private static List<RectInt> BuildUniformGrid(int width, int height, int columns, int rows)
        {
            int cellWidth = width / columns;
            int cellHeight = height / rows;
            var rects = new List<RectInt>();

            for (int row = rows - 1; row >= 0; row--)
            {
                for (int col = 0; col < columns; col++)
                {
                    rects.Add(new RectInt(col * cellWidth, row * cellHeight, cellWidth, cellHeight));
                }
            }

            return rects;
        }

        private static void LogReports(string folderPath, List<SliceReport> reports)
        {
            if (reports.Count == 0)
            {
                Debug.LogWarning($"[HitVfxSpriteSlicer] {folderPath} 底下沒有找到任何貼圖，或全數切割失敗");
                return;
            }

            foreach (SliceReport report in reports)
            {
                string rectsText = string.Join(", ", report.Rects.Select(r => $"({r.x},{r.y},{r.width}x{r.height})"));
                Debug.Log($"[HitVfxSpriteSlicer] {report.AssetPath} -> {report.SpriteCount} 幀，" +
                    $"圖片尺寸 {report.ImageWidth}x{report.ImageHeight}，切割結果：{rectsText}");
            }
        }

        /// <summary>單一貼圖的切割結果，供呼叫端（人工確認流程）檢查。</summary>
        public sealed class SliceReport
        {
            public string AssetPath;
            public int ImageWidth;
            public int ImageHeight;
            public int SpriteCount;
            public List<RectInt> Rects;
        }
    }
}
