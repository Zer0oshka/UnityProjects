using System.Collections.Generic;
using Gameplay.Leaderboard;
using Unity.Entities;
using Unity.MP_FPS;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.MP_FPS.UI
{
    public class LeaderboardUi : MonoBehaviour
    {
        public VisualTreeAsset ScoreItemTemplate;

        EntityManager _entityManager;
        World _currentClientWorld;
        EntityQuery _inGameQuery;
        EntityQuery _networkIdQuery;

        VisualElement _rootElement;
        ListView _listView;
        readonly List<ScoreUiPlayerInfo> _items = new();

        int _localNetworkId = -1;
        float _timer;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            _rootElement = uiDocument.rootVisualElement;
            _rootElement.style.display = DisplayStyle.None;

            _listView = _rootElement.Q<ListView>("score-items");
            _listView.itemsSource = _items;
            _listView.makeItem = () => ScoreItemTemplate.Instantiate();
            _listView.bindItem = BindScoreRow;
            _listView.fixedItemHeight = 28;
            _listView.selectionType = SelectionType.None;
            _listView.RefreshItems();
        }

        void BindScoreRow(VisualElement element, int index)
        {
            var player = _items[index];
            element.Q<Label>("rank").text = player.Rank.ToString();
            element.Q<Label>("name").text = player.PlayerName;
            element.Q<Label>("kills").text = player.Kills.ToString();
            element.Q<Label>("deaths").text = player.Deaths.ToString();
            element.Q<Label>("ratio").text = player.KdRatioText;

            element.EnableInClassList("score-row-local", player.PlayerId == _localNetworkId);
        }

        void OnEnable()
        {
            GameInput.Actions.UI.ShowLeaderboard.started += OnShowLeaderboard;
            GameInput.Actions.UI.ShowLeaderboard.canceled += OnHideLeaderboard;
        }

        void OnDisable()
        {
            GameInput.Actions.UI.ShowLeaderboard.started -= OnShowLeaderboard;
            GameInput.Actions.UI.ShowLeaderboard.canceled -= OnHideLeaderboard;
        }

        void OnShowLeaderboard(InputAction.CallbackContext context)
        {
            if (GameSettings.Instance.IsPauseMenuOpen)
            {
                return;
            }

            _rootElement.style.display = DisplayStyle.Flex;
            UpdateLeaderboardUI();
        }

        void OnHideLeaderboard(InputAction.CallbackContext context)
        {
            _rootElement.style.display = DisplayStyle.None;
        }

        void InitializeClientWorld(World clientWorld)
        {
            if (clientWorld != null)
            {
                _entityManager = clientWorld.EntityManager;
                _inGameQuery = _entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<NetworkStreamInGame>(),
                    ComponentType.ReadOnly<NetworkStreamConnection>()
                );
                _networkIdQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                _rootElement.style.display = DisplayStyle.None;
            }
            else
            {
                _entityManager = default;
                _inGameQuery = default;
                _networkIdQuery = default;
                _localNetworkId = -1;
            }

            _currentClientWorld = clientWorld;
        }

        static World GetClientWorld()
        {
            const string clientWorldName = "ClientWorld";
            for (var i = 0; i < World.All.Count; i++)
            {
                if (World.All[i].Name == clientWorldName && World.All[i].IsCreated)
                {
                    return World.All[i];
                }
            }

            return null;
        }

        void UpdateLocalNetworkId()
        {
            if (_currentClientWorld == null || !_currentClientWorld.IsCreated)
            {
                _localNetworkId = -1;
                return;
            }

            if (!_networkIdQuery.TryGetSingleton<NetworkId>(out var networkId))
            {
                _localNetworkId = -1;
                return;
            }

            _localNetworkId = networkId.Value;
        }

        void Update()
        {
            var clientWorld = GetClientWorld();
            if (clientWorld != _currentClientWorld)
            {
                InitializeClientWorld(clientWorld);
            }

            if (_currentClientWorld == null)
            {
                _rootElement.style.display = DisplayStyle.None;
                return;
            }

            UpdateLocalNetworkId();

            if (_inGameQuery.CalculateEntityCount() == 0)
            {
                _rootElement.style.display = DisplayStyle.None;
                return;
            }

            if (GameSettings.Instance.IsPauseMenuOpen)
            {
                _rootElement.style.display = DisplayStyle.None;
                return;
            }

            if (_rootElement.style.display == DisplayStyle.None)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < 0.5f)
            {
                return;
            }

            _timer = 0.0f;
            UpdateLeaderboardUI();
        }

        void UpdateLeaderboardUI()
        {
            if (LeaderboardManager.Instance == null)
            {
                return;
            }

            var scores = LeaderboardManager.Instance.GetScores();
            scores.Sort((a, b) =>
            {
                var killComparison = b.Kills.CompareTo(a.Kills);
                if (killComparison != 0)
                {
                    return killComparison;
                }

                return a.Deaths.CompareTo(b.Deaths);
            });

            _items.Clear();

            for (var i = 0; i < scores.Count; i++)
            {
                var score = scores[i];
                _items.Add(new ScoreUiPlayerInfo(
                    i + 1,
                    score.NetworkId,
                    score.PlayerName.ToString(),
                    score.Kills,
                    score.Deaths));
            }

            _listView.RefreshItems();
        }
    }

    public class ScoreUiPlayerInfo
    {
        public int Rank;
        public int PlayerId;
        public string PlayerName;
        public int Kills;
        public int Deaths;

        public string KdRatioText
        {
            get
            {
                if (Deaths == 0)
                {
                    return Kills.ToString();
                }

                return (Kills / (float)Deaths).ToString("0.0");
            }
        }

        public ScoreUiPlayerInfo(int rank, int id, string name, int kills, int deaths)
        {
            Rank = rank;
            PlayerId = id;
            PlayerName = name;
            Kills = kills;
            Deaths = deaths;
        }
    }
}
