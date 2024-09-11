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

    public class UploadAvatarEditor : EditorWindow
    {
        private API.Avatar.View.Avatar avatar;
        private DropdownField avatarDropdownField;
        private TextField codeTextField, avatarNameTextField;
        private Exception error;
        private Label errorMessageLabel, avatarInfoLabel, thumbnailLabel, lastUpdateLabel;
        private VisualElement errorPage, loginPage, mainPage, thumbnail;

        private Locale locale;
        private string newThumbnailPath;
        private Button reloadButton, loginButton, selectThumbnailButton, uploadButton, logoutButton, getLoginCodeButton;
        private VRCAvatarDescriptor[] sceneAvatars;
        private VRCAvatarDescriptor selectedAvatar;

        [MenuItem("SEECHAK/Upload Avatar")]
        public static void ShowWindow()
        {
            var window = GetWindow<UploadAvatarEditor>("Upload Avatar");
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

            var uiAsset = Resources.Load<VisualTreeAsset>("UploadAvatarEditor");
            var ui = uiAsset.Instantiate();
            rootVisualElement.Add(ui);

            locale = new Locale();
            locale.Enable();
            locale.SetupLanguageDropdown(rootVisualElement, "LanguageDropdown");

            errorPage = rootVisualElement.Q<VisualElement>("Error");
            loginPage = rootVisualElement.Q<VisualElement>("Login");
            mainPage = rootVisualElement.Q<VisualElement>("Main");
            thumbnail = rootVisualElement.Q<VisualElement>("Thumbnail");

            reloadButton = rootVisualElement.Q<Button>("ReloadButton");
            loginButton = rootVisualElement.Q<Button>("LoginButton");
            selectThumbnailButton = rootVisualElement.Q<Button>("SelectThumbnailButton");
            uploadButton = rootVisualElement.Q<Button>("UploadButton");
            logoutButton = rootVisualElement.Q<Button>("LogoutButton");
            getLoginCodeButton = rootVisualElement.Q<Button>("GetLoginCodeButton");

            codeTextField = rootVisualElement.Q<TextField>("CodeTextField");
            avatarNameTextField = rootVisualElement.Q<TextField>("AvatarNameTextField");

            errorMessageLabel = rootVisualElement.Q<Label>("ErrorMessageLabel");
            avatarInfoLabel = rootVisualElement.Q<Label>("AvatarInfoLabel");
            thumbnailLabel = rootVisualElement.Q<Label>("ThumbnailLabel");
            lastUpdateLabel = rootVisualElement.Q<Label>("LastUpdateLabel");

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

            ShowPage(mainPage);
            logoutButton.style.display = DisplayStyle.Flex;
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
            uploadButton.SetEnabled(enabled);
            logoutButton.SetEnabled(enabled);
            selectThumbnailButton.SetEnabled(enabled);
        }

        private void SetupHandlers()
        {
            EditorApplication.hierarchyChanged += UpdateSceneAvatars;

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

            uploadButton.clicked += async () =>
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
                s => uploadButton.text = s
            );

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
        }
    }
}