using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class HarnessEditorWindow : EditorWindow
{
    private VisualElement _toolsPanel;
    private VisualElement _recipesPanel;
    private DropdownField _toolDropdown;
    private TextField _jsonField;
    private TextField _recipePreviewField;
    private Toggle _allowOverwriteToggle;
    private TextField _statusField;
    private string _lastDisplayedResult = string.Empty;

    [MenuItem("Tools/NPC Harness")]
    public static void Open()
    {
        HarnessEditorWindow window = GetWindow<HarnessEditorWindow>();
        window.titleContent = new GUIContent("NPC Harness");
        window.minSize = new Vector2(620f, 520f);
    }

    public void CreateGUI()
    {
        rootVisualElement.style.paddingLeft = 10f;
        rootVisualElement.style.paddingRight = 10f;
        rootVisualElement.style.paddingTop = 10f;
        rootVisualElement.style.paddingBottom = 10f;

        Label title = new Label("NPC Harness — Deterministic Unity Tools");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 16f;
        title.style.marginBottom = 8f;
        rootVisualElement.Add(title);

        VisualElement navigation = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        navigation.Add(new Button(() => ShowPanel(_toolsPanel)) { text = "Tools" });
        navigation.Add(new Button(() => ShowPanel(_recipesPanel)) { text = "Recipes" });
        rootVisualElement.Add(navigation);

        _allowOverwriteToggle = new Toggle("Allow overwrite of managed values (explicit user approval)");
        _allowOverwriteToggle.style.marginTop = 6f;
        rootVisualElement.Add(_allowOverwriteToggle);

        _toolsPanel = BuildToolsPanel();
        _recipesPanel = BuildRecipesPanel();
        rootVisualElement.Add(_toolsPanel);
        rootVisualElement.Add(_recipesPanel);
        ShowPanel(_toolsPanel);

        Label statusLabel = new Label("Result");
        statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusLabel.style.marginTop = 10f;
        rootVisualElement.Add(statusLabel);
        _statusField = new TextField { multiline = true, isReadOnly = true };
        _statusField.style.height = 100f;
        rootVisualElement.Add(_statusField);
        RefreshLastResult();
    }

    private void OnEnable()
    {
        EditorApplication.update -= RefreshLastResult;
        EditorApplication.update += RefreshLastResult;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RefreshLastResult;
    }

    private VisualElement BuildToolsPanel()
    {
        VisualElement panel = CreatePanel();
        panel.Add(new Label("Run one atomic Tool with structured JSON. Natural language is not accepted here."));

        List<string> toolIds = HarnessToolRegistry.ToolIds.OrderBy(id => id, StringComparer.Ordinal).ToList();
        _toolDropdown = new DropdownField("Tool", toolIds, 0);
        _toolDropdown.RegisterValueChangedCallback(_ => LoadSelectedTemplate());
        panel.Add(_toolDropdown);

        _jsonField = new TextField("Step JSON") { multiline = true };
        _jsonField.style.height = 250f;
        panel.Add(_jsonField);

        VisualElement buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        buttons.Add(new Button(LoadSelectedTemplate) { text = "Load Template" });
        buttons.Add(new Button(ValidateManualStep) { text = "Validate" });
        buttons.Add(new Button(ExecuteManualStep) { text = "Execute" });
        panel.Add(buttons);
        LoadSelectedTemplate();
        return panel;
    }

    private VisualElement BuildRecipesPanel()
    {
        VisualElement panel = CreatePanel();
        panel.Add(new Label(
            "HarnessBeacon composes the same Tools: script → scene → objects → components → material → save."));

        VisualElement buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        buttons.Add(new Button(PreviewHarnessBeacon) { text = "Preview Job" });
        buttons.Add(new Button(RunHarnessBeacon) { text = "Run HarnessBeacon" });
        buttons.Add(new Button(ValidateHarnessBeacon) { text = "Validate Scene" });
        buttons.Add(new Button(RunPlayVerification) { text = "Run Play Verification" });
        panel.Add(buttons);

        _recipePreviewField = new TextField("Job JSON preview") { multiline = true, isReadOnly = true };
        _recipePreviewField.style.height = 300f;
        panel.Add(_recipePreviewField);
        return panel;
    }

    private static VisualElement CreatePanel()
    {
        VisualElement panel = new VisualElement();
        panel.style.marginTop = 10f;
        panel.style.flexGrow = 1f;
        return panel;
    }

    private void ShowPanel(VisualElement selected)
    {
        if (_toolsPanel == null || _recipesPanel == null)
        {
            return;
        }

        _toolsPanel.style.display = selected == _toolsPanel ? DisplayStyle.Flex : DisplayStyle.None;
        _recipesPanel.style.display = selected == _recipesPanel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void LoadSelectedTemplate()
    {
        if (_toolDropdown == null || _jsonField == null || string.IsNullOrEmpty(_toolDropdown.value))
        {
            return;
        }

        _jsonField.value = JsonUtility.ToJson(HarnessToolTemplate.Create(_toolDropdown.value), true);
    }

    private void ValidateManualStep()
    {
        TryCreateManualJob(out HarnessJob job);
        if (job == null)
        {
            return;
        }

        DisplayResult(HarnessJobRunner.Validate(
            job,
            new HarnessExecutionOptions(_allowOverwriteToggle.value, interactive: true)));
    }

    private void ExecuteManualStep()
    {
        TryCreateManualJob(out HarnessJob job);
        if (job == null)
        {
            return;
        }

        string jobPath = HarnessJobStorage.WriteInteractiveJob(job);
        DisplayResult(HarnessJobRunner.RunFromPath(
            jobPath,
            new HarnessExecutionOptions(_allowOverwriteToggle.value, interactive: true)));
    }

    private void PreviewHarnessBeacon()
    {
        HarnessJob job = HarnessBeaconRecipe.Create(CreateJobId("preview"));
        _recipePreviewField.value = JsonUtility.ToJson(job, true);
        DisplayMessage("HarnessBeacon Job preview generated.");
    }

    private void RunHarnessBeacon()
    {
        HarnessJob job = HarnessBeaconRecipe.Create(CreateJobId("beacon"));
        string jobPath = HarnessJobStorage.WriteInteractiveJob(job);
        DisplayResult(HarnessJobRunner.RunFromPath(
            jobPath,
            new HarnessExecutionOptions(_allowOverwriteToggle.value, interactive: true)));
    }

    private void ValidateHarnessBeacon()
    {
        HarnessToolResult result = HarnessBeaconValidator.Validate();
        DisplayMessage($"{result.State}: {result.Message}");
    }

    private void RunPlayVerification()
    {
        HarnessToolResult result = HarnessPlayModeVerifier.StartInteractive();
        DisplayMessage($"{result.State}: {result.Message}");
    }

    private bool TryCreateManualJob(out HarnessJob job)
    {
        job = null;
        try
        {
            HarnessStep step = JsonUtility.FromJson<HarnessStep>(_jsonField.value);
            if (step == null)
            {
                DisplayMessage("Step JSON is empty or invalid.");
                return false;
            }

            step.tool = _toolDropdown.value;
            job = new HarnessJob
            {
                schemaVersion = 1,
                jobId = CreateJobId("manual"),
                steps = new[] { step },
            };
            return true;
        }
        catch (Exception exception)
        {
            DisplayMessage($"Invalid Step JSON: {exception.Message}");
            return false;
        }
    }

    private void RefreshLastResult()
    {
        HarnessJobResult result = HarnessEditorState.GetLastResult();
        if (result == null)
        {
            return;
        }

        string text = $"{result.state}: {result.message}";
        if (text != _lastDisplayedResult)
        {
            DisplayMessage(text);
        }
    }

    private void DisplayResult(HarnessJobResult result)
    {
        DisplayMessage($"{result.state}: {result.message}");
    }

    private void DisplayMessage(string message)
    {
        _lastDisplayedResult = message;
        if (_statusField != null)
        {
            _statusField.value = message;
        }

        Repaint();
    }

    private static string CreateJobId(string prefix)
    {
        return prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    }
}
