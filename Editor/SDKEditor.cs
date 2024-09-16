using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SEECHAK.SDK.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

namespace SEECHAK.SDK.Editor
{
    using API = Core.API;

    public class SDKEditor : EditorWindow
    {
        private API.Avatar.View.Avatar avatar;
        private DropdownField avatarDropdownField;
        private TextField codeTextField, avatarNameTextField;

        private SubPage currentSubPage = SubPage.UploadAvatar;
        private Exception error;
        private Label errorMessageLabel, avatarInfoLabel, thumbnailLabel, lastUpdateLabel, assetUploadDescriptionLabel;
        private VisualElement errorPage, loginPage, mainPage, uploadAssetPage, uploadAvatarPage, thumbnail;

        private Locale locale;
        private string newThumbnailPath;

        private Button reloadButton,
            loginButton,
            selectThumbnailButton,
            avatarUploadButton,
            assetUploadButton,
            logoutButton,
            getLoginCodeButton,
            showUploadAvatarButton,
            showUploadAssetButton;

        private VRCAvatarDescriptor[] sceneAvatars;
        private VRCAvatarDescriptor selectedAvatar;

        [MenuItem("SEECHAK/Show SDK Window")]
        public static void ShowWindow()
        {
            Temp.basePath = AssetDatabase.GUIDToAssetPath("9736c2ae680c1ea4b897edbca087b5ae");
            var window = GetWindow<SDKEditor>("SEECHAK SDK");
            window.Setup();
        }

        private void L(string ko, string en, Action<string> setter)
        {
            locale.L(ko, en, setter);
        }

        private string LL(string ko, string en)
        {
            return locale.LL(ko, en);
        }

        private void Setup()
        {
            API.Client.BaseURL = Config.Value.BaseURL;

            minSize = new Vector2(500, 550);
            maxSize = new Vector2(500, 550);

            var uiAsset = Resources.Load<VisualTreeAsset>("SDKEditor");
            var ui = uiAsset.Instantiate();
            rootVisualElement.Add(ui);

            locale = new Locale();
            locale.Enable();
            locale.SetupLanguageDropdown(rootVisualElement, "LanguageDropdown");

            errorPage = rootVisualElement.Q<VisualElement>("Error");
            loginPage = rootVisualElement.Q<VisualElement>("Login");
            mainPage = rootVisualElement.Q<VisualElement>("Main");
            uploadAssetPage = rootVisualElement.Q<VisualElement>("UploadAsset");
            uploadAvatarPage = rootVisualElement.Q<VisualElement>("UploadAvatar");
            thumbnail = rootVisualElement.Q<VisualElement>("Thumbnail");

            reloadButton = rootVisualElement.Q<Button>("ReloadButton");
            loginButton = rootVisualElement.Q<Button>("LoginButton");
            selectThumbnailButton = rootVisualElement.Q<Button>("SelectThumbnailButton");
            assetUploadButton = uploadAssetPage.Q<Button>("UploadButton");
            avatarUploadButton = uploadAvatarPage.Q<Button>("UploadButton");

            logoutButton = rootVisualElement.Q<Button>("LogoutButton");
            getLoginCodeButton = rootVisualElement.Q<Button>("GetLoginCodeButton");
            showUploadAvatarButton = rootVisualElement.Q<Button>("ShowUploadAvatarButton");
            showUploadAssetButton = rootVisualElement.Q<Button>("ShowUploadAssetButton");

            codeTextField = rootVisualElement.Q<TextField>("CodeTextField");
            avatarNameTextField = rootVisualElement.Q<TextField>("AvatarNameTextField");

            errorMessageLabel = rootVisualElement.Q<Label>("ErrorMessageLabel");
            avatarInfoLabel = rootVisualElement.Q<Label>("AvatarInfoLabel");
            thumbnailLabel = rootVisualElement.Q<Label>("ThumbnailLabel");
            lastUpdateLabel = rootVisualElement.Q<Label>("LastUpdateLabel");
            assetUploadDescriptionLabel = uploadAssetPage.Q<Label>("DescriptionLabel");

            avatarDropdownField = rootVisualElement.Q<DropdownField>("AvatarDropdownField");

            UpdateSceneAvatars();
            SetupHandlers();
            SetupLocale();

            // Start initial setup
            InitializeValues();
            UpdateUIState();
        }

        private void UpdateSceneAvatars()
        {
            sceneAvatars = FindObjectsOfType<VRCAvatarDescriptor>();
            if (!sceneAvatars.Contains(selectedAvatar)) ResetSelectedAvatar();
            avatarDropdownField.choices = sceneAvatars.Select(avatar => avatar.name).ToList();
            UpdateUIState();
        }

        private void InitializeValues()
        {
            newThumbnailPath = null;
            avatarNameTextField.value = "";
            thumbnail.style.backgroundImage = null;
            lastUpdateLabel.style.display = DisplayStyle.None;
        }

        private void ResetSelectedAvatar()
        {
            selectedAvatar = null;
            avatarDropdownField.index = -1;
        }

        private void ShowPage(VisualElement page)
        {
            errorPage.style.display = DisplayStyle.None;
            loginPage.style.display = DisplayStyle.None;
            mainPage.style.display = DisplayStyle.None;
            page.style.display = DisplayStyle.Flex;
        }

        private void ShowSubPage(SubPage page)
        {
            uploadAssetPage.style.display = DisplayStyle.None;
            uploadAvatarPage.style.display = DisplayStyle.None;
            showUploadAssetButton.style.borderTopColor = Color.black * 0.33f;
            showUploadAssetButton.style.borderBottomColor = Color.black * 0.33f;
            showUploadAssetButton.style.borderRightColor = Color.black * 0.33f;
            showUploadAssetButton.style.borderLeftColor = Color.black * 0.33f;
            showUploadAvatarButton.style.borderTopColor = Color.black * 0.33f;
            showUploadAvatarButton.style.borderBottomColor = Color.black * 0.33f;
            showUploadAvatarButton.style.borderRightColor = Color.black * 0.33f;
            showUploadAvatarButton.style.borderLeftColor = Color.black * 0.33f;

            switch (page)
            {
                case SubPage.UploadAvatar:
                    uploadAvatarPage.style.display = DisplayStyle.Flex;
                    showUploadAvatarButton.style.borderTopColor = Color.white * 0.66f;
                    showUploadAvatarButton.style.borderBottomColor = Color.white * 0.66f;
                    showUploadAvatarButton.style.borderRightColor = Color.white * 0.66f;
                    showUploadAvatarButton.style.borderLeftColor = Color.white * 0.66f;
                    break;
                case SubPage.UploadAsset:
                    uploadAssetPage.style.display = DisplayStyle.Flex;
                    showUploadAssetButton.style.borderTopColor = Color.white * 0.66f;
                    showUploadAssetButton.style.borderBottomColor = Color.white * 0.66f;
                    showUploadAssetButton.style.borderRightColor = Color.white * 0.66f;
                    showUploadAssetButton.style.borderLeftColor = Color.white * 0.66f;
                    break;
            }
        }

        private void UpdateUIState()
        {
            if (error != null)
            {
                InitializeValues();
                ResetSelectedAvatar();
                ShowPage(errorPage);
                errorMessageLabel.text = error.Message;
                return;
            }

            if (!API.Client.IsLoggedIn)
            {
                InitializeValues();
                ResetSelectedAvatar();
                logoutButton.style.display = DisplayStyle.None;
                ShowPage(loginPage);
                return;
            }

            logoutButton.style.display = DisplayStyle.Flex;
            ShowPage(mainPage);
            ShowSubPage(currentSubPage);

            switch (currentSubPage)
            {
                case SubPage.UploadAvatar:
                    break;
                case SubPage.UploadAsset:
                    var selected = Selection.gameObjects;
                    if (selected.Length == 0)
                    {
                        assetUploadDescriptionLabel.text = LL(
                            "선택된 오브젝트가 없습니다. 업로드할 오브젝트를 선택해주세요.",
                            "No object selected. Please select object to upload."
                        );
                        assetUploadButton.SetEnabled(false);
                    }
                    else if (selected.Length > 0)
                    {
                        assetUploadDescriptionLabel.text = LL(
                            "<b>선택된 오브젝트: </b>\n",
                            "<b>Selected Object: </b>\n"
                        );
                        assetUploadDescriptionLabel.text += string.Join("\n", selected.Select(e => e.name));
                        assetUploadButton.SetEnabled(true);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task RefreshAvatar(string blueprintId)
        {
            await UniTask.SwitchToMainThread();
            InitializeValues();

            var list = await API.Avatar.List(blueprintId);
            avatar = list.Items.FirstOrDefault();

            if (avatar == null) return;

            await UniTask.SwitchToMainThread();
            avatarNameTextField.value = avatar.Name;
            if (avatar.Thumbnail != null)
            {
                var thumbnailImage = await FetchImageFromUrl(avatar.Thumbnail.URL);
                await UniTask.SwitchToMainThread();
                thumbnail.style.backgroundImage = new StyleBackground(thumbnailImage);
            }

            if (avatar.File != null)
            {
                lastUpdateLabel.style.display = DisplayStyle.Flex;
                lastUpdateLabel.text = LL(
                    "최종 업데이트: ",
                    "Last Update: "
                ) + avatar.File.UploadedAt;
            }
        }

        private async Task<Texture2D> FetchImageFromFile(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > 1024 * 1024 * 10)
            {
                EditorUtility.DisplayDialog("File too large", "Thumbnail must be smaller than 10MB", "OK");
                return null;
            }

            var url = $"file://{path}";
            return await FetchImageFromUrl(url);
        }

        private async Task<Texture2D> FetchImageFromUrl(string url)
        {
            var request = await UnityWebRequestTexture.GetTexture(url).SendWebRequest();
            return ((DownloadHandlerTexture) request.downloadHandler).texture;
        }

        private void SetInputEnabled(bool enabled)
        {
            codeTextField.SetEnabled(enabled);
            avatarNameTextField.SetEnabled(enabled);
            avatarDropdownField.SetEnabled(enabled);
            selectThumbnailButton.SetEnabled(enabled);
            avatarUploadButton.SetEnabled(enabled);
            logoutButton.SetEnabled(enabled);
            selectThumbnailButton.SetEnabled(enabled);
        }

        private void SetupHandlers()
        {
            EditorApplication.hierarchyChanged += UpdateSceneAvatars;

            Selection.selectionChanged += UpdateUIState;

            showUploadAvatarButton.clicked += () =>
            {
                currentSubPage = SubPage.UploadAvatar;
                UpdateUIState();
            };

            showUploadAssetButton.clicked += () =>
            {
                currentSubPage = SubPage.UploadAsset;
                UpdateUIState();
            };

            reloadButton.clicked += () =>
            {
                error = null;
                UpdateUIState();
            };

            loginButton.clicked += async () =>
            {
                try
                {
                    await API.Client.Login(codeTextField.value);
                    error = null;
                }
                catch (Exception e)
                {
                    error = e;
                    Debug.LogError(e);
                }

                await UniTask.SwitchToMainThread();
                UpdateUIState();
            };

            selectThumbnailButton.clicked += async () =>
            {
                var path = EditorUtility.OpenFilePanel("Select Thumbnail", "", "png,jpg,jpeg");
                if (path.Length == 0) return;
                newThumbnailPath = path;
                var texture = await FetchImageFromFile(path);
                if (texture == null) return;
                thumbnail.style.backgroundImage = new StyleBackground(texture);
            };

            avatarUploadButton.clicked += async () =>
            {
                try
                {
                    await UniTask.SwitchToMainThread();
                    SetInputEnabled(false);

                    var blueprintId = selectedAvatar.GetComponent<PipelineManager>().blueprintId;

                    await UploadAvatar.Upload(
                        avatar: selectedAvatar,
                        blueprintId: blueprintId,
                        existingAvatar: avatar,
                        name: avatarNameTextField.value,
                        thumbnailPath: newThumbnailPath
                    );

                    EditorUtility.DisplayDialog("Success", LL(
                        "아바타 업로드가 완료되었습니다.",
                        "Avatar upload completed."
                    ), "OK");
                    await RefreshAvatar(blueprintId);
                }
                catch (Exception e)
                {
                    error = e;
                    Debug.LogError(e);
                }

                await UniTask.SwitchToMainThread();
                SetInputEnabled(true);
                UpdateUIState();
            };

            assetUploadButton.clicked += async () =>
            {
                try
                {
                    await UniTask.SwitchToMainThread();
                    SetInputEnabled(false);

                    var selected = Selection.gameObjects;
                    foreach (var gameObject in selected) await UploadAsset.Upload(gameObject);

                    EditorUtility.DisplayDialog("Success", LL(
                        "에셋 파일 업로드가 완료되었습니다.",
                        "Asset file upload completed."
                    ), "OK");
                }
                catch (Exception e)
                {
                    error = e;
                    Debug.LogError(e);
                }

                await UniTask.SwitchToMainThread();
                SetInputEnabled(true);
                UpdateUIState();
            };

            logoutButton.clicked += () =>
            {
                API.Client.Logout();
                UpdateUIState();
            };

            getLoginCodeButton.clicked += () => { Application.OpenURL($"{Config.Value.WebsiteURL}/my?code"); };

            avatarDropdownField.RegisterValueChangedCallback(async e =>
            {
                if (avatarDropdownField.index < 0) return;
                if (avatarDropdownField.index >= sceneAvatars.Length) return;
                var previousValue = e.previousValue;
                var newAvatar = sceneAvatars[avatarDropdownField.index];
                newAvatar.TryGetComponent<PipelineManager>(out var pipelineManager);
                var blueprintId = pipelineManager != null ? pipelineManager.blueprintId : null;
                if (blueprintId == null || blueprintId.Length == 0)
                {
                    avatarDropdownField.SetValueWithoutNotify(previousValue);
                    EditorUtility.DisplayDialog("Error", LL(
                        "선택한 아바타에 Blueprint ID가 없습니다. VRChat SDK를 통해 먼저 업로드해주세요.",
                        "Selected avatar does not have Blueprint ID. Please upload via VRChat SDK first."
                    ), "OK");
                    return;
                }

                selectedAvatar = newAvatar;

                SetInputEnabled(false);
                try
                {
                    await RefreshAvatar(selectedAvatar.GetComponent<PipelineManager>().blueprintId);
                }
                catch (Exception exception)
                {
                    error = exception;
                }

                await UniTask.SwitchToMainThread();
                SetInputEnabled(true);
            });
        }

        private void SetupLocale()
        {
            L(
                "아바타 업로드",
                "Upload Avatar",
                s => titleContent.text = s
            );

            L(
                "새로고침",
                "Reload",
                s => reloadButton.text = s
            );

            L(
                "로그인",
                "Login",
                s => loginButton.text = s
            );

            L(
                "썸네일",
                "Thumbnail",
                s => thumbnailLabel.text = s
            );

            L(
                "썸네일 선택",
                "Select Thumbnail",
                s => selectThumbnailButton.text = s
            );

            L(
                "업로드",
                "Upload",
                s =>
                {
                    avatarUploadButton.text = s;
                    assetUploadButton.text = s;
                });

            L(
                "로그아웃",
                "Logout",
                s => logoutButton.text = s
            );

            L(
                "로그인 코드",
                "Login Code",
                s => codeTextField.label = s
            );

            L(
                "로그인 코드 발급",
                "Get Login Code",
                s => getLoginCodeButton.text = s
            );

            L(
                "이름",
                "Name",
                s => avatarNameTextField.label = s
            );

            L(
                "아바타 정보",
                "Avatar Info",
                s => avatarInfoLabel.text = s
            );

            L(
                "아바타 선택",
                "Select Avatar",
                s => avatarDropdownField.label = s
            );

            L(
                "최종 업데이트: ",
                "Last Update: ",
                s =>
                {
                    if (avatar?.File == null) return;
                    lastUpdateLabel.text = s + avatar.File.UploadedAt;
                }
            );

            L(
                "아바타 업로드",
                "Upload Avatar",
                s => showUploadAvatarButton.text = s
            );

            L(
                "에셋 파일 업로드",
                "Upload Asset File",
                s => showUploadAssetButton.text = s
            );
        }

        private enum SubPage
        {
            UploadAvatar,
            UploadAsset
        }
    }
}