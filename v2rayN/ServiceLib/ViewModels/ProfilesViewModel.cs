namespace ServiceLib.ViewModels;

public class ProfilesViewModel : MyReactiveObject
{
    #region private prop

    private List<ProfileItem> _lstProfile;
    private string _serverFilter = string.Empty;
    private Dictionary<string, bool> _dicHeaderSort = new();
    private SpeedtestService? _speedtestService;
    private CancellationTokenSource? _autoRealPingCts;

    #endregion private prop

    #region ObservableCollection

    public IObservableCollection<ProfileItemModel> ProfileItems { get; } = new ObservableCollectionExtended<ProfileItemModel>();

    public IObservableCollection<SubItem> SubItems { get; } = new ObservableCollectionExtended<SubItem>();

    [Reactive]
    public ProfileItemModel SelectedProfile { get; set; }

    public IList<ProfileItemModel> SelectedProfiles { get; set; }

    [Reactive]
    public SubItem SelectedSub { get; set; }

    [Reactive]
    public SubItem SelectedMoveToGroup { get; set; }

    [Reactive]
    public string ServerFilter { get; set; }

    #endregion ObservableCollection

    #region Menu

    //servers delete
    public ReactiveCommand<Unit, Unit> EditServerCmd { get; }

    public ReactiveCommand<Unit, Unit> RemoveServerCmd { get; }
    public ReactiveCommand<Unit, Unit> RemoveDuplicateServerCmd { get; }
    public ReactiveCommand<Unit, Unit> CopyServerCmd { get; }
    public ReactiveCommand<Unit, Unit> SetDefaultServerCmd { get; }
    public ReactiveCommand<Unit, Unit> ShareServerCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerXrayRandomCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerXrayRoundRobinCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerXrayLeastPingCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerXrayLeastLoadCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerXrayFallbackCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerSingBoxLeastPingCmd { get; }
    public ReactiveCommand<Unit, Unit> GenGroupMultipleServerSingBoxFallbackCmd { get; }

    //servers move
    public ReactiveCommand<Unit, Unit> MoveTopCmd { get; }

    public ReactiveCommand<Unit, Unit> MoveUpCmd { get; }
    public ReactiveCommand<Unit, Unit> MoveDownCmd { get; }
    public ReactiveCommand<Unit, Unit> MoveBottomCmd { get; }

    //servers ping
    public ReactiveCommand<Unit, Unit> MixedTestServerCmd { get; }

    public ReactiveCommand<Unit, Unit> TcpingServerCmd { get; }
    public ReactiveCommand<Unit, Unit> RealPingServerCmd { get; }
    public ReactiveCommand<Unit, Unit> SpeedServerCmd { get; }
    public ReactiveCommand<Unit, Unit> SortServerResultCmd { get; }
    public ReactiveCommand<Unit, Unit> RemoveInvalidServerResultCmd { get; }
    public ReactiveCommand<Unit, Unit> FastRealPingCmd { get; }

    //servers export
    public ReactiveCommand<Unit, Unit> Export2ClientConfigCmd { get; }

    public ReactiveCommand<Unit, Unit> Export2ClientConfigClipboardCmd { get; }
    public ReactiveCommand<Unit, Unit> Export2ShareUrlCmd { get; }
    public ReactiveCommand<Unit, Unit> Export2ShareUrlBase64Cmd { get; }

    public ReactiveCommand<Unit, Unit> AddSubCmd { get; }
    public ReactiveCommand<Unit, Unit> EditSubCmd { get; }

    #endregion Menu

    #region Init

    public ProfilesViewModel(Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppManager.Instance.Config;
        _updateView = updateView;

        #region WhenAnyValue && ReactiveCommand

        var canEditRemove = this.WhenAnyValue(
           x => x.SelectedProfile,
           selectedSource => selectedSource != null && !selectedSource.IndexId.IsNullOrEmpty());

        this.WhenAnyValue(
            x => x.SelectedSub,
            y => y != null && !y.Remarks.IsNullOrEmpty() && _config.SubIndexId != y.Id)
                .Subscribe(async c => await SubSelectedChangedAsync(c));
        this.WhenAnyValue(
             x => x.SelectedMoveToGroup,
             y => y != null && !y.Remarks.IsNullOrEmpty())
                 .Subscribe(async c => await MoveToGroup(c));

        this.WhenAnyValue(
          x => x.ServerFilter,
          y => y != null && _serverFilter != y)
              .Subscribe(async c => await ServerFilterChanged(c));

        //servers delete
        EditServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditServerAsync(EConfigType.Custom);
        }, canEditRemove);
        RemoveServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RemoveServerAsync();
        }, canEditRemove);
        RemoveDuplicateServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RemoveDuplicateServer();
        });
        CopyServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await CopyServer();
        }, canEditRemove);
        SetDefaultServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SetDefaultServer();
        }, canEditRemove);
        ShareServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ShareServerAsync();
        }, canEditRemove);
        GenGroupMultipleServerXrayRandomCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.Xray, EMultipleLoad.Random);
        }, canEditRemove);
        GenGroupMultipleServerXrayRoundRobinCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.Xray, EMultipleLoad.RoundRobin);
        }, canEditRemove);
        GenGroupMultipleServerXrayLeastPingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.Xray, EMultipleLoad.LeastPing);
        }, canEditRemove);
        GenGroupMultipleServerXrayLeastLoadCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.Xray, EMultipleLoad.LeastLoad);
        }, canEditRemove);
        GenGroupMultipleServerXrayFallbackCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.Xray, EMultipleLoad.Fallback);
        }, canEditRemove);
        GenGroupMultipleServerSingBoxLeastPingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.sing_box, EMultipleLoad.LeastPing);
        }, canEditRemove);
        GenGroupMultipleServerSingBoxFallbackCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await GenGroupMultipleServer(ECoreType.sing_box, EMultipleLoad.Fallback);
        }, canEditRemove);

        //servers move
        MoveTopCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await MoveServer(EMove.Top);
        }, canEditRemove);
        MoveUpCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await MoveServer(EMove.Up);
        }, canEditRemove);
        MoveDownCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await MoveServer(EMove.Down);
        }, canEditRemove);
        MoveBottomCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await MoveServer(EMove.Bottom);
        }, canEditRemove);

        //servers ping
        FastRealPingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ServerSpeedtest(ESpeedActionType.FastRealping);
        });
        MixedTestServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ServerSpeedtest(ESpeedActionType.Mixedtest);
        });
        TcpingServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ServerSpeedtest(ESpeedActionType.Tcping);
        }, canEditRemove);
        RealPingServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ServerSpeedtest(ESpeedActionType.Realping);
        }, canEditRemove);
        SpeedServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ServerSpeedtest(ESpeedActionType.Speedtest);
        }, canEditRemove);
        SortServerResultCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SortServer(EServerColName.DelayVal.ToString());
        });
        RemoveInvalidServerResultCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RemoveInvalidServerResult();
        });
        //servers export
        Export2ClientConfigCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await Export2ClientConfigAsync(false);
        }, canEditRemove);
        Export2ClientConfigClipboardCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await Export2ClientConfigAsync(true);
        }, canEditRemove);
        Export2ShareUrlCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await Export2ShareUrlAsync(false);
        }, canEditRemove);
        Export2ShareUrlBase64Cmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await Export2ShareUrlAsync(true);
        }, canEditRemove);

        //Subscription
        AddSubCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditSubAsync(true);
        });
        EditSubCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditSubAsync(false);
        });

        #endregion WhenAnyValue && ReactiveCommand

        #region AppEvents

        AppEvents.ProfilesRefreshRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await RefreshServersBiz());

        AppEvents.SubscriptionsRefreshRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await RefreshSubscriptions());

        AppEvents.DispatcherStatisticsRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async result => await UpdateStatistics(result));

        AppEvents.SetDefaultServerRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async indexId => await SetDefaultServer(indexId));

        #endregion AppEvents

        _ = Init();
    }

    private async Task Init()
    {
        SelectedProfile = new();
        SelectedSub = new();
        SelectedMoveToGroup = new();

        await RefreshSubscriptions();
        //await RefreshServers();
        
        // 启动自动TCP真连接延迟测试任务
        _ = StartAutoRealPingTask();
    }

    #endregion Init

    #region Actions

    private void Reload()
    {
        AppEvents.ReloadRequested.Publish();
    }

    public async Task SetSpeedTestResult(SpeedTestResult result)
    {
        if (result.IndexId.IsNullOrEmpty())
        {
            NoticeManager.Instance.SendMessageEx(result.Delay);
            NoticeManager.Instance.Enqueue(result.Delay);
            return;
        }
        var item = ProfileItems.FirstOrDefault(it => it.IndexId == result.IndexId);
        if (item == null)
        {
            return;
        }

        if (result.Delay.IsNotEmpty())
        {
            int.TryParse(result.Delay, out var temp);
            item.Delay = temp;
            item.DelayVal = result.Delay ?? string.Empty;
        }
        if (result.Speed.IsNotEmpty())
        {
            item.SpeedVal = result.Speed ?? string.Empty;
        }
    }

    public async Task UpdateStatistics(ServerSpeedItem update)
    {
        if (!_config.GuiItem.EnableStatistics
            || (update.ProxyUp + update.ProxyDown) <= 0
            || DateTime.Now.Second % 3 != 0)
        {
            return;
        }

        try
        {
            var item = ProfileItems.FirstOrDefault(it => it.IndexId == update.IndexId);
            if (item != null)
            {
                item.TodayDown = Utils.HumanFy(update.TodayDown);
                item.TodayUp = Utils.HumanFy(update.TodayUp);
                item.TotalDown = Utils.HumanFy(update.TotalDown);
                item.TotalUp = Utils.HumanFy(update.TotalUp);
            }
        }
        catch
        {
        }
    }

    #endregion Actions

    #region Servers && Groups

    private async Task SubSelectedChangedAsync(bool c)
    {
        if (!c)
        {
            return;
        }
        _config.SubIndexId = SelectedSub?.Id;

        await RefreshServers();

        await _updateView?.Invoke(EViewAction.ProfilesFocus, null);
    }

    private async Task ServerFilterChanged(bool c)
    {
        if (!c)
        {
            return;
        }
        _serverFilter = ServerFilter;
        if (_serverFilter.IsNullOrEmpty())
        {
            await RefreshServers();
        }
    }

    public async Task RefreshServers()
    {
        AppEvents.ProfilesRefreshRequested.Publish();

        await Task.Delay(200);
    }

    private async Task RefreshServersBiz()
    {
        var lstModel = await GetProfileItemsEx(_config.SubIndexId, _serverFilter);
        _lstProfile = JsonUtils.Deserialize<List<ProfileItem>>(JsonUtils.Serialize(lstModel)) ?? [];

        ProfileItems.Clear();
        ProfileItems.AddRange(lstModel);
        if (lstModel.Count > 0)
        {
            var selected = lstModel.FirstOrDefault(t => t.IndexId == _config.IndexId);
            if (selected != null)
            {
                SelectedProfile = selected;
            }
            else
            {
                SelectedProfile = lstModel.First();
            }
        }

        await _updateView?.Invoke(EViewAction.DispatcherRefreshServersBiz, null);
    }

    private async Task RefreshSubscriptions()
    {
        SubItems.Clear();

        SubItems.Add(new SubItem { Remarks = ResUI.AllGroupServers });

        foreach (var item in await AppManager.Instance.SubItems())
        {
            SubItems.Add(item);
        }
        if (_config.SubIndexId != null && SubItems.FirstOrDefault(t => t.Id == _config.SubIndexId) != null)
        {
            SelectedSub = SubItems.FirstOrDefault(t => t.Id == _config.SubIndexId);
        }
        else
        {
            SelectedSub = SubItems.First();
        }
    }

    private async Task<List<ProfileItemModel>?> GetProfileItemsEx(string subid, string filter)
    {
        var lstModel = await AppManager.Instance.ProfileItems(_config.SubIndexId, filter);

        await ConfigHandler.SetDefaultServer(_config, lstModel);

        var lstServerStat = (_config.GuiItem.EnableStatistics ? StatisticsManager.Instance.ServerStat : null) ?? [];
        var lstProfileExs = await ProfileExManager.Instance.GetProfileExs();
        lstModel = (from t in lstModel
                    join t2 in lstServerStat on t.IndexId equals t2.IndexId into t2b
                    from t22 in t2b.DefaultIfEmpty()
                    join t3 in lstProfileExs on t.IndexId equals t3.IndexId into t3b
                    from t33 in t3b.DefaultIfEmpty()
                    select new ProfileItemModel
                    {
                        IndexId = t.IndexId,
                        ConfigType = t.ConfigType,
                        Remarks = t.Remarks,
                        Address = t.Address,
                        Port = t.Port,
                        Security = t.Security,
                        Network = t.Network,
                        StreamSecurity = t.StreamSecurity,
                        Subid = t.Subid,
                        SubRemarks = t.SubRemarks,
                        IsActive = t.IndexId == _config.IndexId,
                        Sort = t33?.Sort ?? 0,
                        Delay = t33?.Delay ?? 0,
                        Speed = t33?.Speed ?? 0,
                        DelayVal = t33?.Delay != 0 ? $"{t33?.Delay}" : string.Empty,
                        SpeedVal = t33?.Speed > 0 ? $"{t33?.Speed}" : t33?.Message ?? string.Empty,
                        TodayDown = t22 == null ? "" : Utils.HumanFy(t22.TodayDown),
                        TodayUp = t22 == null ? "" : Utils.HumanFy(t22.TodayUp),
                        TotalDown = t22 == null ? "" : Utils.HumanFy(t22.TotalDown),
                        TotalUp = t22 == null ? "" : Utils.HumanFy(t22.TotalUp)
                    }).OrderBy(t => t.Sort).ToList();

        return lstModel;
    }

    #endregion Servers && Groups

    #region Add Servers

    private async Task<List<ProfileItem>?> GetProfileItems(bool latest)
    {
        var lstSelected = new List<ProfileItem>();
        if (SelectedProfiles == null || SelectedProfiles.Count <= 0)
        {
            return null;
        }

        var orderProfiles = SelectedProfiles?.OrderBy(t => t.Sort);
        if (latest)
        {
            foreach (var profile in orderProfiles)
            {
                var item = await AppManager.Instance.GetProfileItem(profile.IndexId);
                if (item is not null)
                {
                    lstSelected.Add(item);
                }
            }
        }
        else
        {
            lstSelected = JsonUtils.Deserialize<List<ProfileItem>>(JsonUtils.Serialize(orderProfiles));
        }

        return lstSelected;
    }

    public async Task EditServerAsync(EConfigType eConfigType)
    {
        if (string.IsNullOrEmpty(SelectedProfile?.IndexId))
        {
            return;
        }
        var item = await AppManager.Instance.GetProfileItem(SelectedProfile.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }
        eConfigType = item.ConfigType;

        bool? ret = false;
        if (eConfigType == EConfigType.Custom)
        {
            ret = await _updateView?.Invoke(EViewAction.AddServer2Window, item);
        }
        else if (eConfigType.IsGroupType())
        {
            ret = await _updateView?.Invoke(EViewAction.AddGroupServerWindow, item);
        }
        else
        {
            ret = await _updateView?.Invoke(EViewAction.AddServerWindow, item);
        }
        if (ret == true)
        {
            await RefreshServers();
            if (item.IndexId == _config.IndexId)
            {
                Reload();
            }
        }
    }

    public async Task RemoveServerAsync()
    {
        var lstSelected = await GetProfileItems(true);
        if (lstSelected == null)
        {
            return;
        }
        if (await _updateView?.Invoke(EViewAction.ShowYesNo, null) == false)
        {
            return;
        }
        var exists = lstSelected.Exists(t => t.IndexId == _config.IndexId);

        await ConfigHandler.RemoveServers(_config, lstSelected);
        NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
        if (lstSelected.Count == ProfileItems.Count)
        {
            ProfileItems.Clear();
        }
        await RefreshServers();
        if (exists)
        {
            Reload();
        }
    }

    private async Task RemoveDuplicateServer()
    {
        var tuple = await ConfigHandler.DedupServerList(_config, _config.SubIndexId);
        if (tuple.Item1 > 0 || tuple.Item2 > 0)
        {
            await RefreshServers();
            Reload();
        }
        NoticeManager.Instance.Enqueue(string.Format(ResUI.RemoveDuplicateServerResult, tuple.Item1, tuple.Item2));
    }

    private async Task CopyServer()
    {
        var lstSelected = await GetProfileItems(false);
        if (lstSelected == null)
        {
            return;
        }
        if (await ConfigHandler.CopyServer(_config, lstSelected) == 0)
        {
            await RefreshServers();
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
        }
    }

    public async Task SetDefaultServer()
    {
        if (string.IsNullOrEmpty(SelectedProfile?.IndexId))
        {
            return;
        }
        await SetDefaultServer(SelectedProfile.IndexId);
    }

    private async Task SetDefaultServer(string? indexId)
    {
        if (indexId.IsNullOrEmpty())
        {
            return;
        }
        if (indexId == _config.IndexId)
        {
            return;
        }
        var item = await AppManager.Instance.GetProfileItem(indexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }

        if (await ConfigHandler.SetDefaultServerIndex(_config, indexId) == 0)
        {
            await RefreshServers();
            Reload();
        }
    }

    public async Task ShareServerAsync()
    {
        var item = await AppManager.Instance.GetProfileItem(SelectedProfile.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }
        var url = FmtHandler.GetShareUri(item);
        if (url.IsNullOrEmpty())
        {
            return;
        }

        await _updateView?.Invoke(EViewAction.ShareServer, url);
    }

    private async Task GenGroupMultipleServer(ECoreType coreType, EMultipleLoad multipleLoad)
    {
        var lstSelected = await GetProfileItems(true);
        if (lstSelected == null)
        {
            return;
        }

        var ret = await ConfigHandler.AddGroupServer4Multiple(_config, lstSelected, coreType, multipleLoad, SelectedSub?.Id);
        if (ret.Success != true)
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
            return;
        }
        if (ret?.Data?.ToString() == _config.IndexId)
        {
            await RefreshServers();
            Reload();
        }
        else
        {
            await SetDefaultServer(ret?.Data?.ToString());
        }
    }

    public async Task SortServer(string colName)
    {
        if (colName.IsNullOrEmpty())
        {
            return;
        }

        _dicHeaderSort.TryAdd(colName, true);
        _dicHeaderSort.TryGetValue(colName, out bool asc);
        if (await ConfigHandler.SortServers(_config, _config.SubIndexId, colName, asc) != 0)
        {
            return;
        }
        _dicHeaderSort[colName] = !asc;
        await RefreshServers();
    }

    public async Task RemoveInvalidServerResult()
    {
        var count = await ConfigHandler.RemoveInvalidServerResult(_config, _config.SubIndexId);
        await RefreshServers();
        NoticeManager.Instance.Enqueue(string.Format(ResUI.RemoveInvalidServerResultTip, count));
    }

    //move server
    private async Task MoveToGroup(bool c)
    {
        if (!c)
        {
            return;
        }

        var lstSelected = await GetProfileItems(true);
        if (lstSelected == null)
        {
            return;
        }

        await ConfigHandler.MoveToGroup(_config, lstSelected, SelectedMoveToGroup.Id);
        NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);

        await RefreshServers();
        SelectedMoveToGroup = null;
        SelectedMoveToGroup = new();
    }

    public async Task MoveServer(EMove eMove)
    {
        var item = _lstProfile.FirstOrDefault(t => t.IndexId == SelectedProfile.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }

        var index = _lstProfile.IndexOf(item);
        if (index < 0)
        {
            return;
        }
        if (await ConfigHandler.MoveServer(_config, _lstProfile, index, eMove) == 0)
        {
            await RefreshServers();
        }
    }

    public async Task MoveServerTo(int startIndex, ProfileItemModel targetItem)
    {
        var targetIndex = ProfileItems.IndexOf(targetItem);
        if (startIndex >= 0 && targetIndex >= 0 && startIndex != targetIndex)
        {
            if (await ConfigHandler.MoveServer(_config, _lstProfile, startIndex, EMove.Position, targetIndex) == 0)
            {
                await RefreshServers();
            }
        }
    }

    public async Task ServerSpeedtest(ESpeedActionType actionType)
    {
        if (actionType == ESpeedActionType.Mixedtest)
        {
            SelectedProfiles = ProfileItems;
        }
        else if (actionType == ESpeedActionType.FastRealping)
        {
            SelectedProfiles = ProfileItems;
            actionType = ESpeedActionType.Realping;
        }

        var lstSelected = await GetProfileItems(false);
        if (lstSelected == null)
        {
            return;
        }

        _speedtestService ??= new SpeedtestService(_config, async (SpeedTestResult result) =>
        {
            RxApp.MainThreadScheduler.Schedule(result, (scheduler, result) =>
            {
                _ = SetSpeedTestResult(result);
                return Disposable.Empty;
            });
        });
        _speedtestService?.RunLoop(actionType, lstSelected);
    }

    public void ServerSpeedtestStop()
    {
        _speedtestService?.ExitLoop();
    }

    private async Task Export2ClientConfigAsync(bool blClipboard)
    {
        var item = await AppManager.Instance.GetProfileItem(SelectedProfile.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }

        var msgs = await ActionPrecheckManager.Instance.Check(item);
        if (msgs.Count > 0)
        {
            foreach (var msg in msgs)
            {
                NoticeManager.Instance.SendMessage(msg);
            }
            NoticeManager.Instance.Enqueue(Utils.List2String(msgs.Take(10).ToList(), true));
            return;
        }

        if (blClipboard)
        {
            var result = await CoreConfigHandler.GenerateClientConfig(item, null);
            if (result.Success != true)
            {
                NoticeManager.Instance.Enqueue(result.Msg);
            }
            else
            {
                await _updateView?.Invoke(EViewAction.SetClipboardData, result.Data);
                NoticeManager.Instance.SendMessage(ResUI.OperationSuccess);
            }
        }
        else
        {
            await _updateView?.Invoke(EViewAction.SaveFileDialog, item);
        }
    }

    public async Task Export2ClientConfigResult(string fileName, ProfileItem item)
    {
        if (fileName.IsNullOrEmpty())
        {
            return;
        }
        var result = await CoreConfigHandler.GenerateClientConfig(item, fileName);
        if (result.Success != true)
        {
            NoticeManager.Instance.Enqueue(result.Msg);
        }
        else
        {
            NoticeManager.Instance.SendMessageAndEnqueue(string.Format(ResUI.SaveClientConfigurationIn, fileName));
        }
    }

    public async Task Export2ShareUrlAsync(bool blEncode)
    {
        var lstSelected = await GetProfileItems(true);
        if (lstSelected == null)
        {
            return;
        }

        StringBuilder sb = new();
        foreach (var it in lstSelected)
        {
            var url = FmtHandler.GetShareUri(it);
            if (url.IsNullOrEmpty())
            {
                continue;
            }
            sb.Append(url);
            sb.AppendLine();
        }
        if (sb.Length > 0)
        {
            if (blEncode)
            {
                await _updateView?.Invoke(EViewAction.SetClipboardData, Utils.Base64Encode(sb.ToString()));
            }
            else
            {
                await _updateView?.Invoke(EViewAction.SetClipboardData, sb.ToString());
            }
            NoticeManager.Instance.SendMessage(ResUI.BatchExportURLSuccessfully);
        }
    }

    #endregion Add Servers

    #region Subscription

    private async Task EditSubAsync(bool blNew)
    {
        SubItem item;
        if (blNew)
        {
            item = new();
        }
        else
        {
            item = await AppManager.Instance.GetSubItem(_config.SubIndexId);
            if (item is null)
            {
                return;
            }
        }
        if (await _updateView?.Invoke(EViewAction.SubEditWindow, item) == true)
        {
            await RefreshSubscriptions();
            await SubSelectedChangedAsync(true);
        }
    }

    #endregion Subscription

    #region Auto Real Ping Test

    /// <summary>
    /// 启动自动TCP真连接延迟测试任务
    /// </summary>
    private async Task StartAutoRealPingTask()
    {
        _autoRealPingCts?.Cancel();
        _autoRealPingCts = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            var numOfExecuted = 1;
            while (!_autoRealPingCts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000 * 60, _autoRealPingCts.Token); // 每分钟检查一次
                numOfExecuted++;
                
                // 检查是否启用自动测试
                if (!_config.SpeedTestItem.AutoRealPingTest)
                {
                    continue;
                }
                
                // 检查测试间隔
                if (_config.SpeedTestItem.AutoRealPingTestInterval <= 0)
                {
                    continue;
                }
                
                // 检查是否到达测试间隔
                if (numOfExecuted % _config.SpeedTestItem.AutoRealPingTestInterval != 0)
                {
                    continue;
                }
                
                // 执行自动TCP真连接延迟测试
                await AutoRealPingTest();
            }
        }, _autoRealPingCts.Token);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 执行自动TCP真连接延迟测试并重新排序
    /// </summary>
    private async Task AutoRealPingTest()
    {
        try
        {
            // 检查是否有服务器需要测试
            if (ProfileItems.Count == 0)
            {
                return;
            }
            
            // 发送通知
            NoticeManager.Instance.SendMessageEx("开始自动TCP真连接延迟测试...");
            
            // 执行TCP真连接延迟测试
            await ServerSpeedtest(ESpeedActionType.Realping);
            
            // 等待测试完成
            await Task.Delay(5000);
            
            // 按照延迟最短重新排序
            await SortServersByDelay();
            
            // 检查并切换当前活动服务器（如果不可用）
            await CheckAndSwitchCurrentServer();
            
            NoticeManager.Instance.SendMessageEx("自动TCP真连接延迟测试完成");
        }
        catch (Exception ex)
        {
            NoticeManager.Instance.SendMessageEx($"自动TCP真连接延迟测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查当前活动服务器是否可用，如果不可用则切换到可用服务器中延迟最低的一个
    /// </summary>
    private async Task CheckAndSwitchCurrentServer()
    {
        try
        {
            // 获取当前活动服务器
            var currentServer = await ConfigHandler.GetDefaultServer(_config);
            if (currentServer == null)
            {
                return;
            }
            
            // 在ProfileItems中查找当前服务器的延迟信息
            var currentProfileItem = ProfileItems.FirstOrDefault(p => p.IndexId == currentServer.IndexId);
            if (currentProfileItem == null)
            {
                return;
            }
            
            // 判断服务器是否可用：延迟大于0且小于超时时间（通常超时时间设为5000ms）
            bool isCurrentServerAvailable = currentProfileItem.Delay > 0 && currentProfileItem.Delay < 5000;
            
            if (!isCurrentServerAvailable)
            {
                // 当前服务器不可用，查找可用服务器中延迟最低的一个
                var availableServers = ProfileItems
                    .Where(p => p.Delay > 0 && p.Delay < 5000) // 延迟在合理范围内
                    .OrderBy(p => p.Delay) // 按延迟升序排序
                    .ToList();
                
                if (availableServers.Count > 0)
                {
                    var bestServer = availableServers.First();
                    
                    // 切换到最佳可用服务器
                    await SetDefaultServer(bestServer.IndexId);
                    
                    NoticeManager.Instance.SendMessageEx($"当前服务器不可用，已自动切换到延迟最低的可用服务器: {bestServer.Remarks} (延迟: {bestServer.Delay}ms)");
                }
                else
                {
                    NoticeManager.Instance.SendMessageEx("当前服务器不可用，但未找到其他可用服务器");
                }
            }
            // 如果当前服务器可用，则不进行切换
        }
        catch (Exception ex)
        {
            NoticeManager.Instance.SendMessageEx($"检查并切换服务器失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 按照延迟最短重新排序服务器
    /// </summary>
    private async Task SortServersByDelay()
    {
        try
        {
            // 将所有服务器分为三类进行排序：
            // 1. 可用服务器（Delay > 0）：按延迟从小到大排序
            // 2. 未测试服务器（Delay = 0）：排在可用服务器之后
            // 3. 连接失败的服务器（Delay = -1）：排在最后
            var sortedServers = ProfileItems
                .OrderBy(p => 
                {
                    // 第一优先级：服务器状态
                    if (p.Delay > 0) return 1;  // 可用服务器排第一
                    if (p.Delay == 0) return 2; // 未测试服务器排第二
                    return 3;                   // 连接失败的服务器排最后
                })
                .ThenBy(p => p.Delay) // 第二优先级：延迟值（仅对可用服务器有效）
                .ToList();
            
            // 更新所有服务器的排序
            for (int i = 0; i < sortedServers.Count; i++)
            {
                var server = sortedServers[i];
                ProfileExManager.Instance.SetSort(server.IndexId, i + 1);
            }
            
            // 保存排序结果
            await ProfileExManager.Instance.SaveTo();
            
            // 刷新服务器列表显示
            await RefreshServers();
        }
        catch (Exception ex)
        {
            NoticeManager.Instance.SendMessageEx($"服务器排序失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止自动TCP真连接延迟测试
    /// </summary>
    public void StopAutoRealPingTask()
    {
        _autoRealPingCts?.Cancel();
        _autoRealPingCts = null;
    }

    #endregion Auto Real Ping Test

}
