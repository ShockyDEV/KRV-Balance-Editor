using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BalanceEditor
{
    public partial class MainForm
    {
        // ══════════════════════════════════════════════════
        //  ANDROID IMPORT / EXPORT
        // ══════════════════════════════════════════════════

        static readonly string KRV_PACKAGE = "com.ironhidegames.android.kingdomrush4";

        string androidWorkDir;

        // ── Tool discovery ──

        string FindAdbPath()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sdkAdb = Path.Combine(localApp, @"Android\Sdk\platform-tools\adb.exe");
            if (File.Exists(sdkAdb)) return sdkAdb;

            string progAdb = @"C:\Android\platform-tools\adb.exe";
            if (File.Exists(progAdb)) return progAdb;

            // Search PATH
            string[] pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';');
            foreach (var dir in pathDirs)
            {
                string p = Path.Combine(dir.Trim(), "adb.exe");
                if (File.Exists(p)) return p;
            }
            return null;
        }

        string FindApksignerPath()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string buildTools = Path.Combine(localApp, @"Android\Sdk\build-tools");
            if (!Directory.Exists(buildTools)) return null;

            return Directory.GetDirectories(buildTools)
                .OrderByDescending(d => d)
                .Select(d => Path.Combine(d, "apksigner.bat"))
                .FirstOrDefault(f => File.Exists(f));
        }

        string FindJavaPath()
        {
            // Check JAVA_HOME
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHome))
            {
                string p = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(p)) return p;
            }

            // Search PATH
            string[] pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';');
            foreach (var dir in pathDirs)
            {
                string p = Path.Combine(dir.Trim(), "java.exe");
                if (File.Exists(p)) return p;
            }

            // Common install locations
            foreach (var root in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                string javaDir = Path.Combine(root, "Java");
                if (Directory.Exists(javaDir))
                {
                    foreach (var d in Directory.GetDirectories(javaDir).OrderByDescending(x => x))
                    {
                        string p = Path.Combine(d, "bin", "java.exe");
                        if (File.Exists(p)) return p;
                    }
                }
            }
            return null;
        }

        string FindZipalignPath()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string buildTools = Path.Combine(localApp, @"Android\Sdk\build-tools");
            if (!Directory.Exists(buildTools)) return null;

            return Directory.GetDirectories(buildTools)
                .OrderByDescending(d => d)
                .Select(d => Path.Combine(d, "zipalign.exe"))
                .FirstOrDefault(f => File.Exists(f));
        }

        string FindApktoolJar()
        {
            // 1. Inside tool's APK_MOD
            if (androidWorkDir != null)
            {
                string p = Path.Combine(androidWorkDir, "apktool.jar");
                if (File.Exists(p)) return p;
            }

            // 2. Game root's APK_MOD (from manual work)
            string gameApk = Path.Combine(gameRoot, "APK_MOD", "apktool.jar");
            if (File.Exists(gameApk))
            {
                // Copy to tool's APK_MOD for future use
                if (androidWorkDir != null)
                {
                    string dest = Path.Combine(androidWorkDir, "apktool.jar");
                    try { File.Copy(gameApk, dest, false); } catch { }
                    return dest;
                }
                return gameApk;
            }

            // 3. Next to the editor exe
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string exeJar = Path.Combine(exeDir, "apktool.jar");
            if (File.Exists(exeJar)) return exeJar;

            return null;
        }

        // ── Process runner ──

        (int ExitCode, string Output) RunCmd(string exe, string args, int timeoutMs = 60000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(timeoutMs);
                return (proc.ExitCode, (stdout + "\n" + stderr).Trim());
            }
        }

        void SetStatus(string text)
        {
            lblStatus.Text = text;
            Application.DoEvents();
        }

        // ══════════════════════════════════════════════════
        //  PATCH BASE APK → DEBUGGABLE (apktool + zipalign)
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Decodes base.apk with apktool, injects android:debuggable="true"
        /// into the manifest, rebuilds, zipaligns, and replaces base.apk.
        /// This enables "adb shell run-as" for save-file access.
        /// </summary>
        void PatchBaseApkDebuggable(string java, string apktoolJar, string zipalign)
        {
            string baseApk = Path.Combine(androidWorkDir, "base.apk");
            string decodedDir = Path.Combine(androidWorkDir, "base_decoded");
            string debugApk = Path.Combine(androidWorkDir, "base_debuggable.apk");
            string alignedApk = Path.Combine(androidWorkDir, "base_aligned.apk");

            // Decode (keep classes.dex intact with -s flag)
            SetStatus("Patching APK (decoding)...");
            var decResult = RunCmd(java,
                $"-jar \"{apktoolJar}\" d -s -f \"{baseApk}\" -o \"{decodedDir}\"", 180000);
            if (decResult.ExitCode != 0)
                throw new Exception($"apktool decode failed:\n{decResult.Output}");

            // Patch AndroidManifest.xml to add debuggable flag
            SetStatus("Patching APK (manifest)...");
            string manifestPath = Path.Combine(decodedDir, "AndroidManifest.xml");
            string manifest = File.ReadAllText(manifestPath);
            if (!manifest.Contains("android:debuggable=\"true\""))
            {
                manifest = manifest.Replace("<application ",
                    "<application android:debuggable=\"true\" ");
                File.WriteAllText(manifestPath, manifest);
            }

            // Rebuild with apktool
            SetStatus("Patching APK (rebuilding)...");
            var buildResult = RunCmd(java,
                $"-jar \"{apktoolJar}\" b \"{decodedDir}\" -o \"{debugApk}\"", 180000);
            if (buildResult.ExitCode != 0)
                throw new Exception($"apktool rebuild failed:\n{buildResult.Output}");

            // Zipalign (required for Android R+ — resources.arsc must be uncompressed & 4-byte aligned)
            SetStatus("Patching APK (aligning)...");
            var alignResult = RunCmd(zipalign,
                $"-f -p 4 \"{debugApk}\" \"{alignedApk}\"", 30000);
            if (alignResult.ExitCode != 0)
                throw new Exception($"zipalign failed:\n{alignResult.Output}");

            // Replace base.apk with the patched + aligned version
            File.Copy(alignedApk, baseApk, true);

            // Cleanup intermediate files
            try { File.Delete(debugApk); } catch { }
            try { File.Delete(alignedApk); } catch { }
            try { Directory.Delete(decodedDir, true); } catch { }
        }

        // ══════════════════════════════════════════════════
        //  AUTO-UNLOCK HEROES & TOWERS (silent, after push)
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Silently attempts to unlock all heroes/towers by patching the save
        /// file via run-as.  Returns true if unlocked (or already unlocked),
        /// false if the save file isn't available yet.
        /// </summary>
        bool AutoUnlockHeroes(string adb)
        {
            try
            {
                var lsResult = RunCmd(adb,
                    $"shell run-as {KRV_PACKAGE} ls files/", 5000);

                // APK not debuggable or no save yet → nothing to do
                if (lsResult.Output.Contains("not debuggable") ||
                    !lsResult.Output.Contains("save_data.plist"))
                    return false;

                SetStatus("Auto-unlocking heroes & towers...");
                string saveContent = PullFileViaRunAs(
                    adb, KRV_PACKAGE, "files/save_data.plist");

                if (string.IsNullOrEmpty(saveContent) ||
                    !saveContent.Contains("<key>purchased</key>"))
                    return false;

                int beforeTrue = Regex.Matches(saveContent,
                    @"<key>purchased</key>\s*<true\s*/>").Count;

                string modified = Regex.Replace(saveContent,
                    @"(<key>purchased</key>\s*)<false\s*/>", "$1<true/>");

                // Already fully unlocked
                if (modified == saveContent) return true;

                PushFileViaRunAs(adb, KRV_PACKAGE,
                    "files/save_data.plist", modified);

                int afterTrue = Regex.Matches(modified,
                    @"<key>purchased</key>\s*<true\s*/>").Count;
                SetStatus($"Auto-unlocked {afterTrue - beforeTrue} heroes/towers");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════
        //  PUSH TO ANDROID
        // ══════════════════════════════════════════════════

        void AndroidPush_Click(object sender, EventArgs e)
        {
            // ── Find core tools ──
            string adb = FindAdbPath();
            if (adb == null)
            {
                MessageBox.Show(
                    "ADB not found.\n\nInstall Android SDK Platform Tools and ensure adb.exe is accessible.",
                    "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string apksigner = FindApksignerPath();
            if (apksigner == null)
            {
                MessageBox.Show(
                    "apksigner not found.\n\nInstall Android SDK Build Tools (via Android Studio SDK Manager).",
                    "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ── Check device ──
            var devResult = RunCmd(adb, "devices");
            if (!devResult.Output.Contains("\tdevice"))
            {
                MessageBox.Show(
                    "No Android device connected.\n\nConnect your phone via USB and enable USB Debugging in Developer Options.",
                    "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── Setup work directory ──
            androidWorkDir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath), "APK_MOD");
            Directory.CreateDirectory(androidWorkDir);

            string keystorePath = Path.Combine(androidWorkDir, "debug.keystore");
            string splitsFile = Path.Combine(androidWorkDir, "splits.txt");

            // ── Pull APKs if not cached ──
            List<string> splitNames;
            bool needPull = !File.Exists(splitsFile);

            if (needPull)
            {
                // Check KRV is installed on device
                var pmResult = RunCmd(adb, $"shell pm path {KRV_PACKAGE}");
                if (pmResult.ExitCode != 0 || !pmResult.Output.Contains("package:"))
                {
                    MessageBox.Show(
                        "Kingdom Rush Vengeance not found on device.",
                        "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (MessageBox.Show(
                    "First-time setup: need to pull the APK from your phone (~1.1 GB) and patch it.\n" +
                    "This only happens once — everything is cached for future pushes.\n\nContinue?",
                    "Android Export", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                var remotePaths = pmResult.Output
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => l.StartsWith("package:"))
                    .Select(l => l.Substring(8).Trim())
                    .ToList();

                splitNames = new List<string>();
                Cursor = Cursors.WaitCursor;

                foreach (var remotePath in remotePaths)
                {
                    string filename = Path.GetFileName(remotePath);
                    string localPath = Path.Combine(androidWorkDir, filename);

                    SetStatus($"Pulling {filename}...");
                    var pullResult = RunCmd(adb,
                        $"pull \"{remotePath}\" \"{localPath}\"", 300000);
                    if (pullResult.ExitCode != 0)
                    {
                        Cursor = Cursors.Default;
                        SetStatus("Pull failed");
                        MessageBox.Show($"Failed to pull {filename}:\n{pullResult.Output}",
                            "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    splitNames.Add(filename);
                }

                // Generate signing keystore (one-time)
                if (!File.Exists(keystorePath))
                {
                    SetStatus("Generating signing key...");
                    RunCmd("keytool",
                        $"-genkey -v -keystore \"{keystorePath}\" -alias debug -keyalg RSA -keysize 2048 " +
                        "-validity 10000 -storepass android -keypass android " +
                        "-dname \"CN=KRVMod,O=BalanceEditor,C=US\"");
                }

                // Save splits manifest
                File.WriteAllLines(splitsFile, splitNames);
                Cursor = Cursors.Default;
            }
            else
            {
                splitNames = File.ReadAllLines(splitsFile)
                    .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            }

            // ── Ensure base.apk is debuggable (independent of pull — runs once) ──
            string debugMarker = Path.Combine(androidWorkDir, "debuggable.marker");
            if (!File.Exists(debugMarker))
            {
                string java = FindJavaPath();
                string zipalign = FindZipalignPath();
                string apktoolJar = FindApktoolJar();

                if (java == null || zipalign == null || apktoolJar == null)
                {
                    string missing = "";
                    if (java == null) missing += "• Java  —  https://adoptium.net/\n";
                    if (zipalign == null) missing += "• zipalign  —  Android SDK Build Tools\n";
                    if (apktoolJar == null) missing += "• apktool.jar  —  https://apktool.org/  (place in APK_MOD folder)\n";

                    MessageBox.Show(
                        "Tools needed to make the APK debuggable (for hero unlock):\n\n" + missing +
                        "\nBalance modding will still work, but auto-unlock won't.\n" +
                        "Install the missing tools and push again to enable it.",
                        "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Continue without patching — balance changes still work
                }
                else
                {
                    Cursor = Cursors.WaitCursor;
                    try
                    {
                        PatchBaseApkDebuggable(java, apktoolJar, zipalign);

                        // Re-sign base.apk after patching
                        SetStatus("Signing patched base.apk...");
                        RunCmd(apksigner,
                            $"sign --ks \"{keystorePath}\" --ks-pass pass:android " +
                            $"--key-pass pass:android --ks-key-alias debug " +
                            $"\"{Path.Combine(androidWorkDir, "base.apk")}\"", 30000);

                        File.WriteAllText(debugMarker, DateTime.Now.ToString("o"));
                    }
                    catch (Exception ex)
                    {
                        SetStatus("Debuggable patch failed");
                        MessageBox.Show(
                            $"Failed to patch APK as debuggable:\n\n{ex.Message}\n\n" +
                            "Balance modding will still work, but auto hero unlock won't.\n" +
                            "You can retry with Data > Clear APK Cache, then Push again.",
                            "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    Cursor = Cursors.Default;
                }
            }

            // ── Ensure all non-asset APKs are signed (one-time after pull or patch) ──
            string signedMarker = Path.Combine(androidWorkDir, "signed.marker");
            if (!File.Exists(signedMarker))
            {
                Cursor = Cursors.WaitCursor;
                foreach (var name in splitNames.Where(n => !n.Contains("kr4_assets")))
                {
                    SetStatus($"Signing {name}...");
                    RunCmd(apksigner,
                        $"sign --ks \"{keystorePath}\" --ks-pass pass:android " +
                        $"--key-pass pass:android --ks-key-alias debug " +
                        $"\"{Path.Combine(androidWorkDir, name)}\"", 30000);
                }
                File.WriteAllText(signedMarker, DateTime.Now.ToString("o"));
                Cursor = Cursors.Default;
            }

            // ── Find the assets APK ──
            string assetsApkName = splitNames.FirstOrDefault(n => n.Contains("kr4_assets"));
            if (assetsApkName == null)
            {
                MessageBox.Show("Could not find kr4_assets split in cached APKs.",
                    "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string assetsApkPath = Path.Combine(androidWorkDir, assetsApkName);
            string moddedApkPath = Path.Combine(androidWorkDir, "split_kr4_assets_modded.apk");

            // ── Create modded APK ──
            Cursor = Cursors.WaitCursor;
            SetStatus("Creating modded APK...");

            File.Copy(assetsApkPath, moddedApkPath, true);

            try
            {
                // Generate free products JSON (all heroes & towers cost 0)
                string freeProductsPath = Path.Combine(androidWorkDir, "fullads_products_free.json");
                GenerateFreeProductsJson(freeProductsPath);

                // ── Normalize modded plists to the Android engine's schema ──
                // The Steam (PC) balance can be a patch ahead of the Android build, so its
                // plists may contain new array elements, new dict keys, or new field types
                // the older Android engine can't parse. Writing those straight into the APK
                // triggers a crash the moment the game reads the unknown structure (e.g.
                // trying to iterate effects[2] when the engine only knows effects[0..1]).
                // So: for each plist, diff the PC shape against the APK's original shape
                // and revert any purely-structural delta, while keeping user's numeric
                // edits anywhere the leaf exists in both. The push is now safe on both
                // game versions.
                SetStatus("Normalizing plists for Android engine...");
                string androidTowers = Path.Combine(androidWorkDir, "towers_settings.android.plist");
                string androidUnits  = Path.Combine(androidWorkDir, "units_settings.android.plist");
                NormalizePlistForAndroid(assetsApkPath,
                    "assets/KR4/Settings/towers_settings.plist",
                    towersFilePath, androidTowers);
                NormalizePlistForAndroid(assetsApkPath,
                    "assets/KR4/Settings/units_settings.plist",
                    unitsFilePath, androidUnits);

                using (var zip = ZipFile.Open(moddedApkPath, ZipArchiveMode.Update))
                {
                    ReplaceZipEntry(zip, "assets/KR4/Settings/towers_settings.plist", androidTowers);
                    ReplaceZipEntry(zip, "assets/KR4/Settings/units_settings.plist", androidUnits);
                    ReplaceZipEntry(zip, "assets/KR4/Settings/fullads_products.json", freeProductsPath);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                SetStatus("Mod failed");
                MessageBox.Show($"Failed to modify APK:\n{ex.Message}",
                    "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ── Sign modded APK ──
            SetStatus("Signing modded APK...");
            RunCmd(apksigner,
                $"sign --ks \"{keystorePath}\" --ks-pass pass:android " +
                $"--key-pass pass:android --ks-key-alias debug \"{moddedApkPath}\"", 60000);

            // ── Disable Play Protect (prevents "harmful app" block) ──
            SetStatus("Disabling Play Protect...");
            RunCmd(adb, "shell settings put global package_verifier_enable 0", 5000);
            RunCmd(adb, "shell settings put global verifier_verify_adb_installs 0", 5000);

            // ── Install: try in-place replace first (preserves saves) ──
            var installFiles = splitNames
                .Where(n => !n.Contains("kr4_assets"))
                .Select(n => $"\"{Path.Combine(androidWorkDir, n)}\"")
                .ToList();
            installFiles.Add($"\"{moddedApkPath}\"");
            string installArgs = string.Join(" ", installFiles);

            SetStatus("Installing modded APK (keeping saves)...");
            var installResult = RunCmd(adb,
                $"install-multiple -r {installArgs}", 120000);

            bool freshInstall = false;

            if (!installResult.Output.Contains("Success"))
            {
                // Signature mismatch (first mod) — must uninstall the Play Store version
                var choice = MessageBox.Show(
                    "First-time mod: the original game must be uninstalled (different signing key).\n\n" +
                    "Your saves will be lost, but heroes will be auto-unlocked\n" +
                    "after you play one level and push again.\n\nContinue?",
                    "Android Export", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (choice != DialogResult.Yes)
                {
                    Cursor = Cursors.Default;
                    SetStatus("Install cancelled");
                    return;
                }

                SetStatus("Uninstalling original...");
                RunCmd(adb, $"uninstall {KRV_PACKAGE}", 15000);

                SetStatus("Installing modded APK...");
                installResult = RunCmd(adb,
                    $"install-multiple {installArgs}", 120000);
                freshInstall = true;
            }

            Cursor = Cursors.Default;

            if (installResult.Output.Contains("Success"))
            {
                // ── Auto-unlock heroes & towers (if save file exists) ──
                bool unlocked = AutoUnlockHeroes(adb);

                string msg;
                if (freshInstall)
                {
                    msg = "Modded KRV installed on your phone!\n\n" +
                        "Since this was a fresh install, you need to:\n" +
                        "  1. Open the game and complete one level\n" +
                        "  2. Come back here and use Data > Unlock All Heroes/Towers\n\n" +
                        "Future pushes will preserve your saves and auto-unlock automatically.";
                }
                else if (unlocked)
                {
                    msg = "Modded KRV updated on your phone!\n\n" +
                        "  Saves preserved\n  Heroes/towers auto-unlocked\n\n" +
                        "Open the game to test your balance changes.";
                }
                else
                {
                    msg = "Modded KRV updated on your phone!\n\n" +
                        "Saves preserved. Open the game to test your balance changes.\n\n" +
                        "Tip: Use Data > Unlock All Heroes/Towers if you need to re-unlock content.";
                }

                SetStatus("Android export complete!");
                MessageBox.Show(msg, "Android Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SetStatus("Install failed");
                MessageBox.Show($"Install failed:\n{installResult.Output}",
                    "Android Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════
        //  PULL FROM ANDROID
        // ══════════════════════════════════════════════════

        void AndroidPull_Click(object sender, EventArgs e)
        {
            string adb = FindAdbPath();
            if (adb == null)
            {
                MessageBox.Show("ADB not found.", "Android Import",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var devResult = RunCmd(adb, "devices");
            if (!devResult.Output.Contains("\tdevice"))
            {
                MessageBox.Show(
                    "No Android device connected.\n\nConnect your phone via USB and enable USB Debugging.",
                    "Android Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find assets APK on device
            var pmResult = RunCmd(adb, $"shell pm path {KRV_PACKAGE}");
            var assetsRemote = pmResult.Output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.Contains("kr4_assets"))
                .Select(l => l.Substring(8).Trim())
                .FirstOrDefault();

            if (assetsRemote == null)
            {
                MessageBox.Show("KRV assets APK not found on device.",
                    "Android Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show(
                "Import balance files from phone?\n\n" +
                "This will extract the phone's balance data into Android mode.\n" +
                "Your PC balance files will NOT be modified.",
                "Android Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            androidWorkDir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath), "APK_MOD");
            Directory.CreateDirectory(androidWorkDir);

            string tempApk = Path.Combine(androidWorkDir, "temp_import.apk");

            Cursor = Cursors.WaitCursor;
            SetStatus("Pulling assets from phone...");

            var pullResult = RunCmd(adb, $"pull \"{assetsRemote}\" \"{tempApk}\"", 300000);
            if (pullResult.ExitCode != 0)
            {
                Cursor = Cursors.Default;
                SetStatus("Pull failed");
                MessageBox.Show($"Failed to pull APK:\n{pullResult.Output}",
                    "Android Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetStatus("Extracting balance files...");

            try
            {
                string androidTowers = Path.Combine(androidWorkDir, "towers_settings.plist");
                string androidUnits = Path.Combine(androidWorkDir, "units_settings.plist");

                using (var zip = ZipFile.OpenRead(tempApk))
                {
                    var towersEntry = zip.GetEntry("assets/KR4/Settings/towers_settings.plist");
                    var unitsEntry = zip.GetEntry("assets/KR4/Settings/units_settings.plist");

                    if (towersEntry == null || unitsEntry == null)
                    {
                        Cursor = Cursors.Default;
                        SetStatus("Import failed");
                        MessageBox.Show("Settings plists not found in APK.",
                            "Android Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Extract to APK_MOD folder (never touch PC files)
                    towersEntry.ExtractToFile(androidTowers, true);
                    unitsEntry.ExtractToFile(androidUnits, true);
                }

                // Switch to Android mode
                towersFilePath = androidTowers;
                unitsFilePath = androidUnits;
                isAndroidMode = true;

                LoadAllData();
                Changes.Clear();

                // Update platform ComboBox without triggering event
                cbPlatform.SelectedIndexChanged -= CbPlatform_Changed;
                cbPlatform.SelectedItem = "Android";
                cbPlatform.SelectedIndexChanged += CbPlatform_Changed;

                string cat = cbCategory?.SelectedItem?.ToString() ?? "Towers";
                if (cat == "Balance Status")
                {
                    HideTierList();
                    ShowTierList();
                }
                else
                    PopulateListBox(cat);
                ClearDetail();
                UpdatePlatformUI();

                Cursor = Cursors.Default;
                SetStatus("Android import complete");
                MessageBox.Show(
                    "Balance files imported from phone!\n\n" +
                    "You are now in ANDROID mode.\n" +
                    "Edits will only affect the Android copy — your PC files are untouched.\n" +
                    "Use 'Push to Android' to deploy changes to your phone.",
                    "Android Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                SetStatus("Import failed");
                MessageBox.Show($"Failed to extract:\n{ex.Message}",
                    "Android Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { File.Delete(tempApk); } catch { }
            }
        }

        // ══════════════════════════════════════════════════
        //  CLEAR APK CACHE
        // ══════════════════════════════════════════════════

        void AndroidClearCache_Click(object sender, EventArgs e)
        {
            androidWorkDir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath), "APK_MOD");

            if (!Directory.Exists(androidWorkDir))
            {
                MessageBox.Show("No APK cache found.", "Android",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Clear cached APK files?\n\nThe APK will be re-pulled from the phone on the next export.\n" +
                "Use this if you updated KRV from the Play Store.",
                "Android", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Directory.Delete(androidWorkDir, true);
                MessageBox.Show("APK cache cleared.", "Android",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear cache:\n{ex.Message}",
                    "Android", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ──

        void GenerateFreeProductsJson(string outputPath)
        {
            // Read original products JSON from game data to preserve structure
            string srcJson = Path.Combine(gameRoot, @"KR4\Settings\fullads_products.json");
            if (File.Exists(srcJson))
            {
                // Simple regex replace: set all price_crowns and price_gems to 0
                string json = File.ReadAllText(srcJson);
                json = Regex.Replace(json, @"""price_crowns""\s*:\s*\d+", @"""price_crowns"":0");
                json = Regex.Replace(json, @"""price_gems""\s*:\s*\d+", @"""price_gems"":0");
                File.WriteAllText(outputPath, json);
            }
            else
            {
                // Fallback: generate minimal free products file
                File.WriteAllText(outputPath, @"{
    ""products"" : {
        ""hero_margosa""           : { ""key_prefix"":""HERO_MARGOSA""          , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_tramin""            : { ""key_prefix"":""HERO_TRAMIN""           , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_mortemis""          : { ""key_prefix"":""HERO_MORTEMIS""         , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_jigou""             : { ""key_prefix"":""HERO_JIGOU""            , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_beresad""           : { ""key_prefix"":""HERO_BERESAD""          , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_tank""              : { ""key_prefix"":""HERO_TANK""             , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_naga""              : { ""key_prefix"":""HERO_NAGA""             , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_eiskalt""           : { ""key_prefix"":""HERO_EISKALT""          , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_murglun""           : { ""key_prefix"":""HERO_MURGLUN""          , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""hero_jack_o_lantern""    : { ""key_prefix"":""HERO_JACK_O_LANTERN""   , ""type"":""hero"" , ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_zepelling""        : { ""key_prefix"":""TOWER_WAR_ZEPELLING""   , ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_grim_cemetery""    : { ""key_prefix"":""TOWER_GRIM_CEMETERY""   , ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_blazing_watcher""  : { ""key_prefix"":""TOWER_BLAZING_WATCHER"" , ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_rotten_forest""    : { ""key_prefix"":""TOWER_ROTTEN_FOREST""   , ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_wicked_sisters""   : { ""key_prefix"":""TOWER_WICKED_SISTERS""  , ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_deep_devils""      : { ""key_prefix"":""TOWER_DEEP_DEVILS_REEF"", ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 },
        ""tower_swamp_monster""    : { ""key_prefix"":""TOWER_SWAMP_MONSTER""   , ""type"":""tower"", ""price_crowns"":0, ""price_gems"":0, ""crowns_lease_seconds"":86400 }
    }
}");
            }
        }

        void ReplaceZipEntry(ZipArchive zip, string entryPath, string sourceFile)
        {
            var existing = zip.GetEntry(entryPath);
            if (existing != null) existing.Delete();
            zip.CreateEntryFromFile(sourceFile, entryPath, CompressionLevel.NoCompression);
        }

        /// <summary>
        /// Produce an Android-safe copy of <paramref name="moddedPlistPath"/> by
        /// pulling the original plist out of <paramref name="sourceApkPath"/>,
        /// diffing structures, and writing a normalized plist to
        /// <paramref name="outPath"/>. Keeps every numeric edit the user made
        /// while stripping PC-only array growths / new keys that would crash
        /// the older Android engine.
        /// </summary>
        void NormalizePlistForAndroid(string sourceApkPath, string entryInsideApk,
            string moddedPlistPath, string outPath)
        {
            // Read the APK's original plist into memory
            System.Xml.Linq.XDocument originalDoc;
            using (var zip = System.IO.Compression.ZipFile.OpenRead(sourceApkPath))
            {
                var entry = zip.GetEntry(entryInsideApk);
                if (entry == null)
                    throw new System.IO.FileNotFoundException(
                        $"Entry '{entryInsideApk}' not found in {sourceApkPath}");
                using (var s = entry.Open())
                    originalDoc = System.Xml.Linq.XDocument.Load(s);
            }

            // Load the modded (PC-shaped) plist
            var moddedDoc = System.Xml.Linq.XDocument.Load(moddedPlistPath);

            // Normalize shape in-place on moddedDoc
            var (kept, reverted) = PlistHelper.NormalizeAgainst(moddedDoc, originalDoc);
            System.Diagnostics.Debug.WriteLine(
                $"[android-normalize] {System.IO.Path.GetFileName(moddedPlistPath)}: " +
                $"kept {kept} numeric edits, reverted {reverted} structural deltas");

            // Write to outPath with the full plist header
            string header =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" " +
                "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n";
            string xml = moddedDoc.Root.ToString();
            System.IO.File.WriteAllText(outPath, header + xml + "\n",
                System.Text.Encoding.UTF8);
        }

        // ══════════════════════════════════════════════════
        //  UNLOCK ALL HEROES & TOWERS (Android — manual)
        // ══════════════════════════════════════════════════

        void AndroidUnlockAll_Click(object sender, EventArgs e)
        {
            string adb = FindAdbPath();
            if (adb == null)
            {
                MessageBox.Show("ADB not found.", "Unlock All",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var devResult = RunCmd(adb, "devices");
            if (!devResult.Output.Contains("\tdevice"))
            {
                MessageBox.Show("No Android device connected.",
                    "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if run-as works (APK must be debuggable)
            var lsResult = RunCmd(adb, $"shell run-as {KRV_PACKAGE} ls files/");
            if (lsResult.Output.Contains("not debuggable"))
            {
                MessageBox.Show(
                    "The installed game is not debuggable.\n\n" +
                    "You need to install the debuggable build first via 'Push to Android'.\n" +
                    "(The first push patches the APK to enable save file access.)",
                    "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!lsResult.Output.Contains("save_data.plist"))
            {
                MessageBox.Show(
                    "No save file found on the device.\n\n" +
                    "Open the game on your phone and complete at least one level first.",
                    "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor = Cursors.WaitCursor;
            try
            {
                // Pull save via run-as
                SetStatus("Reading save data...");
                string saveContent = PullFileViaRunAs(adb, KRV_PACKAGE, "files/save_data.plist");

                if (string.IsNullOrEmpty(saveContent) || !saveContent.Contains("<key>purchased</key>"))
                {
                    Cursor = Cursors.Default;
                    SetStatus("No purchase data found");
                    MessageBox.Show("Save file has no purchase flags.",
                        "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Count existing unlocks
                int beforeTrue = Regex.Matches(saveContent, @"<key>purchased</key>\s*<true\s*/>").Count;

                // Replace all purchased=false → purchased=true
                SetStatus("Unlocking all heroes and towers...");
                string modified = Regex.Replace(saveContent,
                    @"(<key>purchased</key>\s*)<false\s*/>", "$1<true/>");

                int afterTrue = Regex.Matches(modified, @"<key>purchased</key>\s*<true\s*/>").Count;
                int unlocked = afterTrue - beforeTrue;

                if (unlocked == 0)
                {
                    Cursor = Cursors.Default;
                    SetStatus("Already unlocked");
                    MessageBox.Show("All heroes and towers are already unlocked!",
                        "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Push modified save back
                SetStatus("Saving unlocked data...");
                PushFileViaRunAs(adb, KRV_PACKAGE, "files/save_data.plist", modified);

                Cursor = Cursors.Default;
                SetStatus($"Unlocked {unlocked} items!");
                MessageBox.Show(
                    $"Done! Unlocked {unlocked} heroes/towers.\n\n" +
                    "Open the game to enjoy all content!",
                    "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                SetStatus("Unlock failed");
                MessageBox.Show($"Failed to unlock:\n{ex.Message}",
                    "Unlock All", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── run-as file access (requires debuggable APK) ──

        string PullFileViaRunAs(string adb, string package, string remotePath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = adb,
                Arguments = $"exec-out run-as {package} cat {remotePath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi))
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(30000);
                return output;
            }
        }

        void PushFileViaRunAs(string adb, string package, string remotePath, string content)
        {
            var psi = new ProcessStartInfo
            {
                FileName = adb,
                Arguments = $"shell \"run-as {package} sh -c 'cat > {remotePath}'\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi))
            {
                byte[] data = Encoding.UTF8.GetBytes(content);
                proc.StandardInput.BaseStream.Write(data, 0, data.Length);
                proc.StandardInput.BaseStream.Flush();
                proc.StandardInput.Close();
                proc.WaitForExit(30000);
            }
        }
    }
}
