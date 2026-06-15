using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using BloxHive.Models;
using BloxHive.ViewModels;

namespace BloxHive.Services;

public static partial class DashboardService
{
    private static HttpListener? _listener;
    private static CancellationTokenSource? _cts;
    private static string _authToken = "";
    private static string _passwordHash = "";
    private static int _port = 5000;

    private static bool _isLocalOnly;

    static DashboardService()
    {
        TunnelService.UrlChanged += url => TunnelUrlChanged?.Invoke(url);
        TunnelService.StatusChanged += msg => TunnelStatusChanged?.Invoke(msg);
    }

    public static event Action<string>? TunnelStatusChanged;

    public static bool IsRunning => _listener?.IsListening ?? false;
    public static bool IsNetworkAccessible => !_isLocalOnly;
    public static string LocalUrl => $"http://localhost:{_port}";
    public static string NetworkUrl
    {
        get
        {
            if (_isLocalOnly) return LocalUrl;
            var host = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
            return host != null ? $"http://{host}:{_port}" : LocalUrl;
        }
    }

    public static string? PublicUrl => TunnelService.PublicUrl;

    public static event Action<bool>? RunningChanged;
    public static event Action<string?>? TunnelUrlChanged;

    public static string Start(int port, string passwordHash)
    {
        if (IsRunning) Stop();

        _port = port;
        _passwordHash = passwordHash;
        _cts = new CancellationTokenSource();
        _isLocalOnly = false;

        _listener = new HttpListener();
        try
        {
            _listener.Prefixes.Add($"http://+:{port}/");
            _listener.Start();
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 5)
        {
            _listener.Close();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _isLocalOnly = true;
        }

        _ = TunnelService.StartAsync(port);
        _ = Run(_cts.Token);
        RunningChanged?.Invoke(true);
        return _isLocalOnly ? $"http://localhost:{port}" : $"http://+:{port}";
    }

    public static void Stop()
    {
        _ = TunnelService.StopAsync();
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;
        _authToken = "";
        RunningChanged?.Invoke(false);
    }

    private static async Task Run(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener!.GetContextAsync().WaitAsync(ct);
                _ = HandleRequest(ctx);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch { }
        }
    }

    private static async Task HandleRequest(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath?.TrimEnd('/') ?? "/";
        var method = ctx.Request.HttpMethod.ToUpperInvariant();

        try
        {
            if (path == "/api/login" && method == "POST")
                await HandleLogin(ctx);
            else if (path == "/api/status" && method == "GET")
                await HandleGetStatus(ctx);
            else if (path == "/api/kill-all" && method == "POST")
                await HandleKillAll(ctx);
            else if (Regex.IsMatch(path, @"^/api/kill/\d+$") && method == "POST")
                await HandleKill(ctx, int.Parse(path.Split('/')[3]));
            else if (path == "/api/multi-instance" && method == "POST")
                await HandleMultiInstance(ctx);
            else if (path == "/api/loop" && method == "POST")
                await HandleLoopToggle(ctx);
            else if (path == "/api/webhook-test" && method == "POST")
                await HandleWebhookTest(ctx);
            else if (path == "/api/process-webhook" && method == "POST")
                await HandleProcessWebhook(ctx);
            else if (path == "/api/account-open" && method == "POST")
                await HandleAccountOpen(ctx);
            else if (path is "" or "/" or "/login")
                await ServeFile(ctx.Response, "text/html; charset=utf-8", GetLoginHtml(Translation.Instance.IsEnglish));
            else if (path == "/dashboard")
                await ServeFile(ctx.Response, "text/html; charset=utf-8", _dashboardHtml);
            else if (path == "/style.css")
                await ServeFile(ctx.Response, "text/css; charset=utf-8", _styleCss);
            else if (path == "/script.js")
                await ServeFile(ctx.Response, "application/javascript; charset=utf-8", _scriptJs);
            else if (path == "/qrcode.min.js")
                await ServeFile(ctx.Response, "application/javascript; charset=utf-8", _qrcodeJs);
            else
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
            }
        }
        catch { ctx.Response.StatusCode = 500; ctx.Response.Close(); }
    }

    private static bool CheckAuth(HttpListenerRequest req)
    {
        if (string.IsNullOrEmpty(_passwordHash)) return true;
        var auth = req.Headers["Authorization"];
        return auth == $"Bearer {_authToken}";
    }

    private static async Task HandleLogin(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (string.IsNullOrEmpty(_passwordHash))
        {
            await RespondJson(res, new { success = true, token = "" });
            return;
        }

        var body = await ReadBody(req);
        using var doc = JsonDocument.Parse(body);
        var password = doc.RootElement.GetProperty("password").GetString() ?? "";
        var hash = ToSha256(password);

        if (hash == _passwordHash)
        {
            _authToken = Guid.NewGuid().ToString("N");
            await RespondJson(res, new { success = true, token = _authToken });
        }
        else
        {
            await RespondJson(res, new { success = false, error = "Wrong password" });
        }
    }

    private static async Task HandleGetStatus(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (!CheckAuth(req)) { await RespondUnauthorized(res); return; }

        var settings = SettingsService.Load();
        var processes = HomeViewModel.GetProcessList();
        var accounts = HomeViewModel.GetAccountList();

        var accent = "#F59E0B";
        var bgDark = "#0D0D0D";
        var bgCard = "#1A1A1A";
        var bgTertiary = "#252525";
        var textPrimary = "#FFFFFF";
        var textSecondary = "#888888";
        var textMuted = "#555555";
        var lang = Translation.Instance.IsEnglish ? "en" : "de";
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var themeDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.ToString()?.Contains("Styles/Themes/") == true);
                if (themeDict != null)
                {
                    if (themeDict.Contains("Accent")) { var c = (System.Windows.Media.Color)themeDict["Accent"]; accent = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                    if (themeDict.Contains("SurfaceDark")) { var c = (System.Windows.Media.Color)themeDict["SurfaceDark"]; bgDark = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                    if (themeDict.Contains("SurfaceDarkSecondary")) { var c = (System.Windows.Media.Color)themeDict["SurfaceDarkSecondary"]; bgCard = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                    if (themeDict.Contains("SurfaceDarkTertiary")) { var c = (System.Windows.Media.Color)themeDict["SurfaceDarkTertiary"]; bgTertiary = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                    if (themeDict.Contains("TextPrimary")) { var c = (System.Windows.Media.Color)themeDict["TextPrimary"]; textPrimary = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                    if (themeDict.Contains("TextSecondary")) { var c = (System.Windows.Media.Color)themeDict["TextSecondary"]; textSecondary = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                    if (themeDict.Contains("TextMuted")) { var c = (System.Windows.Media.Color)themeDict["TextMuted"]; textMuted = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
                }
            });
        }
        catch { }

        var accountsData = accounts.Select(a => new
        {
            displayName = a.DisplayName,
            userId = a.UserId,
            statusText = a.StatusText,
            isOnline = a.IsOnline
        }).ToList();

        await RespondJson(res, new
        {
            processes = processes.Select(p => new { id = p.Id, displayName = p.DisplayName, isWebhookEnabled = p.IsWebhookEnabled }).ToList(),
            accounts = accountsData,
            loopActive = HomeViewModel.GetLoopActive(),
            multiInstanceActive = MutexService.Instance.IsActive,
            webhookUrl = !string.IsNullOrEmpty(settings.WebhookUrl),
            url = PublicUrl ?? NetworkUrl,
            tunnelUrl = PublicUrl,
            language = lang,
            theme = new { accent, bgDark, bgCard, bgTertiary, textPrimary, textSecondary, textMuted }
        });
    }

    private static async Task HandleKill(HttpListenerContext ctx, int pid)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (!CheckAuth(req)) { await RespondUnauthorized(res); return; }
        new RobloxProcessService().Kill(pid);
        await RespondJson(res, new { success = true });
    }

    private static async Task HandleKillAll(HttpListenerContext ctx)
    {
        var res = ctx.Response;
        if (!CheckAuth(ctx.Request)) { await RespondUnauthorized(res); return; }
        new RobloxProcessService().KillAll();
        await RespondJson(res, new { success = true });
    }

    private static async Task HandleMultiInstance(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (!CheckAuth(req)) { await RespondUnauthorized(res); return; }
        var body = await ReadBody(req);
        using var doc = JsonDocument.Parse(body);
        var active = doc.RootElement.GetProperty("active").GetBoolean();

        if (active) MutexService.Instance.Acquire();
        else MutexService.Instance.Release();

        var settings = SettingsService.Load();
        settings.MultiInstanceActive = MutexService.Instance.IsActive;
        SettingsService.Save(settings);

        await RespondJson(res, new { success = true, active = MutexService.Instance.IsActive });
    }

    private static async Task HandleLoopToggle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (!CheckAuth(req)) { await RespondUnauthorized(res); return; }
        var body = await ReadBody(req);
        using var doc = JsonDocument.Parse(body);
        var active = doc.RootElement.GetProperty("active").GetBoolean();

        HomeViewModel.ExternalToggleLoop(active);
        await RespondJson(res, new { success = true, active });
    }

    private static async Task HandleWebhookTest(HttpListenerContext ctx)
    {
        var res = ctx.Response;
        if (!CheckAuth(ctx.Request)) { await RespondUnauthorized(res); return; }
        var settings = SettingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.WebhookUrl))
        {
            await RespondJson(res, new { success = false, error = "No webhook URL" });
            return;
        }
        var ok = await new WebhookService().SendTest(settings.WebhookUrl);
        await RespondJson(res, new { success = ok });
    }

    private static async Task HandleProcessWebhook(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (!CheckAuth(req)) { await RespondUnauthorized(res); return; }
        try
        {
            var body = await ReadBody(req);
            using var doc = JsonDocument.Parse(body);
            var pid = doc.RootElement.GetProperty("pid").GetInt32();
            var enabled = doc.RootElement.GetProperty("enabled").GetBoolean();
            Debug.WriteLine($"[DashboardService] Webhook: PID={pid}, Enabled={enabled}");
            HomeViewModel.SetProcessWebhook(pid, enabled);
            await RespondJson(res, new { success = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardService] Webhook FEHLER: {ex.Message}");
            ctx.Response.StatusCode = 500;
            await RespondJson(ctx.Response, new { success = false, error = ex.Message });
        }
    }

    private static async Task HandleAccountOpen(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (!CheckAuth(req)) { await RespondUnauthorized(res); return; }
        var body = await ReadBody(req);
        using var doc = JsonDocument.Parse(body);
        var userId = doc.RootElement.GetProperty("userId").GetInt64();
        var account = AccountService.Load().FirstOrDefault(a => a.UserId == userId);
        if (account != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new Views.AccountWindow(account);
                window.Show();
            });
            await RespondJson(res, new { success = true });
        }
        else
        {
            await RespondJson(res, new { success = false, error = "Account not found" });
        }
    }

    private static async Task RespondJson(HttpListenerResponse res, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private static async Task RespondUnauthorized(HttpListenerResponse res)
    {
        res.StatusCode = 401;
        await RespondJson(res, new { error = "Unauthorized" });
    }

    private static async Task<string> ReadBody(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task ServeFile(HttpListenerResponse res, string contentType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        res.ContentType = contentType;
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    public static string ToSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GetLoginHtml(bool isEnglish)
    {
        var lang = isEnglish ? "en" : "de";
        return $$"""
<!DOCTYPE html>
<html lang="{{lang}}">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>BloxHive Dashboard</title><link rel="stylesheet" href="/style.css"></head>
<body>
<div class="login-box">
  <div class="logo">B</div>
  <h1>BloxHive Dashboard</h1>
  <p data-i18n="enterPassword">Passwort eingeben</p>
  <input type="password" id="password" placeholder="Passwort" autofocus>
  <button onclick="login()" data-i18n="login">Anmelden</button>
  <p id="error" class="error"></p>
</div>
<script>
var _i18n={
  de:{enterPassword:'Passwort eingeben',login:'Anmelden',wrongPassword:'Falsches Passwort',connectionError:'Verbindungsfehler',placeholder:'Passwort'},
  en:{enterPassword:'Enter password',login:'Login',wrongPassword:'Wrong password',connectionError:'Connection error',placeholder:'Password'}
};
var _lang='{{lang}}';
function __(k){return (_i18n[_lang]&&_i18n[_lang][k])||(_i18n.de[k])||k;}
function _i18nApply(){document.querySelectorAll('[data-i18n]').forEach(function(e){var k=e.getAttribute('data-i18n');if(k)e.textContent=__(k)})}
function login(){
  var p=document.getElementById('password').value;
  fetch('/api/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({password:p})})
  .then(function(r){return r.json()})
  .then(function(d){if(d.success){localStorage.setItem('token',d.token);window.location='/dashboard'}else document.getElementById('error').textContent=__('wrongPassword')})
  .catch(function(){document.getElementById('error').textContent=__('connectionError')});
}
document.addEventListener('DOMContentLoaded',function(){document.getElementById('password').placeholder=__('placeholder');_i18nApply()});
document.getElementById('password').addEventListener('keydown',function(e){if(e.key==='Enter')login()});
</script>
</body>
</html>
""";
    }

    private const string _dashboardHtml = """
<!DOCTYPE html>
<html lang="de">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>BloxHive Dashboard</title>
<link rel="stylesheet" href="/style.css">
</head>
<body>
<div class="container">
  <header>
    <div class="brand"><div class="logo">B</div><h1>BloxHive</h1></div>
    <span class="badge" id="badge">LIVE</span>
  </header>

  <main>
    <section class="card card-full" id="processes-card">
      <div class="card-header">
        <h2><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--accent)" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg> <span data-i18n="processes">Prozesse</span></h2>
        <button class="btn btn-danger btn-sm" id="killAllBtn" onclick="killAllFn()" data-i18n="killAll">Alle killen</button>
      </div>
      <div id="processes"><p class="empty" data-i18n="loading">Lade...</p></div>
    </section>

    <section class="card card-full" id="accounts-card">
      <div class="card-header">
        <h2><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--accent)" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg> <span data-i18n="accounts">Accounts</span></h2>
      </div>
      <div id="accounts"><p class="empty" data-i18n="loading">Lade...</p></div>
    </section>

    <section class="card" id="actions-card">
      <div class="card-header">
        <h2><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--accent)" stroke-width="2"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg> <span data-i18n="actions">Aktionen</span></h2>
      </div>
      <div class="actions-list">
        <div class="action-row">
          <div class="action-info"><span class="action-label">Loop</span><span class="action-desc" data-i18n="loopDesc">Automatische Screenshots</span></div>
          <button class="toggle-btn" id="btnLoop" onclick="toggleLoopFn()">AUS</button>
        </div>
        <div class="action-row">
          <div class="action-info"><span class="action-label">Multi-Instance</span><span class="action-desc" data-i18n="multiDesc">Mehrere Instanzen</span></div>
          <button class="toggle-btn" id="btnMulti" onclick="toggleMultiFn()">AUS</button>
        </div>
        <div class="action-row">
          <div class="action-info"><span class="action-label">Webhook</span><span class="action-desc" data-i18n="webhookTest">Test-Webhook senden</span></div>
          <button class="btn btn-accent btn-sm" id="webhookTestBtn" onclick="webhookTestFn()"><span data-i18n="test">Test</span></button>
        </div>
      </div>
    </section>

    <section class="card" id="access-card">
      <div class="card-header">
        <h2><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--accent)" stroke-width="2"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg> <span data-i18n="access">Zugang</span></h2>
      </div>
      <div class="access-content">
        <div id="qr-container"></div>
        <p id="urlDisplay"></p>
        <p class="hint" data-i18n="qrHint">Im selben Netzwerk auf deinem Smartphone öffnen</p>
      </div>
    </section>
  </main>
</div>
<script src="/qrcode.min.js"></script>
<script>
var _token=localStorage.getItem('token');
if(!_token)window.location='/login';
var _state=null,_lang='de',_busy=false;
var _i18n={
  de:{on:'EIN',off:'AUS',online:'Online',offline:'Offline',noStatus:'Kein Status',noProcesses:'Keine laufenden Prozesse',noAccounts:'Keine Accounts gespeichert',loading:'Lade...',killConfirm:'Alle Prozesse wirklich killen?',webhookOk:'Webhook gesendet!',webhookFail:'Fehler',killAll:'Alle killen',test:'Test',processes:'Prozesse',accounts:'Accounts',actions:'Aktionen',access:'Zugang',loopDesc:'Automatische Screenshots',multiDesc:'Mehrere Instanzen',webhookTest:'Test-Webhook senden',qrHint:'Im selben Netzwerk auf deinem Smartphone öffnen'},
  en:{on:'ON',off:'OFF',online:'Online',offline:'Offline',noStatus:'No status',noProcesses:'No running processes',noAccounts:'No saved accounts',loading:'Loading...',killConfirm:'Really kill all processes?',webhookOk:'Webhook sent!',webhookFail:'Error',killAll:'Kill All',test:'Test',processes:'Processes',accounts:'Accounts',actions:'Actions',access:'Access',loopDesc:'Auto screenshots',multiDesc:'Multiple instances',webhookTest:'Send test webhook',qrHint:'Open on your phone in the same network'}
};
function __(k){return (_i18n[_lang]&&_i18n[_lang][k])||(_i18n.de[k])||k;}
function _api(p,b){return fetch(p,{method:b?'POST':'GET',headers:{'Content-Type':'application/json','Authorization':'Bearer '+_token},body:b?JSON.stringify(b):null});}
function _theme(t){
  if(!t)return;var s=document.documentElement.style;
  if(t.accent) s.setProperty('--accent',t.accent);if(t.bgDark)s.setProperty('--bg-dark',t.bgDark);
  if(t.bgCard)s.setProperty('--bg-card',t.bgCard);if(t.bgTertiary)s.setProperty('--bg-tertiary',t.bgTertiary);
  if(t.textPrimary)s.setProperty('--text-primary',t.textPrimary);if(t.textSecondary)s.setProperty('--text-secondary',t.textSecondary);if(t.textMuted)s.setProperty('--text-muted',t.textMuted);
}
function _i18nApply(){var e=document.querySelectorAll('[data-i18n]');for(var i=0;i<e.length;i++){var k=e[i].getAttribute('data-i18n');if(k)e[i].textContent=__(k);}}
function esc(s){if(s==null)return'';var d=document.createElement('div');d.textContent=s;return d.innerHTML;}

function _render(){
  if(!_state||_busy)return;
  try{
    var pe=document.getElementById('processes');
    if(pe){
      if(_state.processes&&_state.processes.length){
        var h='';
        for(var i=0;i<_state.processes.length;i++){
          var p=_state.processes[i];
          h+='<div class="item">'+
            '<input type="checkbox" class="wp-cb" data-pid="'+p.id+'"'+(p.isWebhookEnabled?' checked':'')+' onchange="window.handleWebhookChange(this)">'+
            '<div class="item-body"><span class="item-name">'+esc(p.displayName)+'</span><span class="item-pid">PID '+p.id+'</span></div>'+
            '<button class="btn btn-icon kill-btn" data-kill="'+p.id+'" title="Kill"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>'+
          '</div>';
        }
        pe.innerHTML=h;
      }else pe.innerHTML='<p class="empty">'+__('noProcesses')+'</p>';
    }
    var ae=document.getElementById('accounts');
    if(ae){
      if(_state.accounts&&_state.accounts.length){
        var h='';
        for(var i=0;i<_state.accounts.length;i++){
          var a=_state.accounts[i];
          var dc=a.isOnline?'online':'offline';
          var st=a.isOnline?__('online'):__('offline');
          if(a.statusText) st+=' &middot; '+esc(a.statusText);
          h+='<div class="item">'+
            '<span class="dot '+dc+'"></span>'+
            '<div class="item-body"><span class="item-name">'+esc(a.displayName)+'</span><span class="item-status">'+st+'</span></div>'+
          '</div>';
        }
        ae.innerHTML=h;
      }else ae.innerHTML='<p class="empty">'+__('noAccounts')+'</p>';
    }
    var lb=document.getElementById('btnLoop');if(lb){lb.textContent=_state.loopActive?__('on'):__('off');lb.className='toggle-btn'+(_state.loopActive?' active':'');}
    var mb=document.getElementById('btnMulti');if(mb){mb.textContent=_state.multiInstanceActive?__('on'):__('off');mb.className='toggle-btn'+(_state.multiInstanceActive?' active':'');}
    var bg=document.getElementById('badge');if(bg)bg.style.background=_state.loopActive?'var(--success,#22C55E)':'var(--accent,#F59E0B)';
  }catch(e){console.error('render',e);}
}

window.handleWebhookChange = function(cb) {
  if (_busy) return;
  _busy = true;
  
  var pid = parseInt(cb.getAttribute('data-pid')), en = cb.checked;
  console.log('Webhook Change getriggert:', pid, en);
  
  if (_state && _state.processes) {
    for (var j = 0; j < _state.processes.length; j++) {
      if (_state.processes[j].id === pid) {
        _state.processes[j].isWebhookEnabled = en;
        break;
      }
    }
  }
  
  _api('/api/process-webhook', { pid: pid, enabled: en })
    .then(function(r) {
      if (!r.ok) throw new Error('HTTP ' + r.status);
      return r.json();
    })
    .then(function(d) {
      if (!d.success) throw new Error('Server lehnte ab');
      console.log('Webhook API erfolgreich.');
      _busy = false;
      setTimeout(function() { _load(true); }, 100);
    })
    .catch(function(e) {
      console.error('Webhook API Fehler:', e);
      cb.checked = !en;
      if (_state && _state.processes) {
        for (var j = 0; j < _state.processes.length; j++) {
          if (_state.processes[j].id === pid) {
            _state.processes[j].isWebhookEnabled = !en;
            break;
          }
        }
      }
      _busy = false;
      _render();
    });
};

function killAllFn(){if(confirm(__('killConfirm')))_api('/api/kill-all',{}).then(function(){setTimeout(function(){ _load(true); },600);}).catch(function(){setTimeout(function(){ _load(true); },1000);});}
function webhookTestFn(){_api('/api/webhook-test',{}).then(function(r){return r.json();}).then(function(d){alert(d.success?__('webhookOk'):__('webhookFail'));}).catch(function(){alert(__('webhookFail'));});}
function toggleLoopFn(){_api('/api/loop',{active:!_state.loopActive}).then(function(){ _load(true); }).catch(function(){setTimeout(function(){ _load(true); },1000);});}
function toggleMultiFn(){_api('/api/multi-instance',{active:!_state.multiInstanceActive}).then(function(){ _load(true); }).catch(function(){setTimeout(function(){ _load(true); },1000);});}

document.addEventListener('click',function(e){
  var kb=e.target.closest('.kill-btn');
  if(kb){
    _api('/api/kill/'+kb.getAttribute('data-kill'),{}).then(function(){setTimeout(function(){ _load(true); },600);}).catch(function(){setTimeout(function(){ _load(true); },1000);});
  }
});

var _loadTimer = null;

async function _load(isManualForce) {
  if (isManualForce && _loadTimer) {
    clearTimeout(_loadTimer);
  }

  if (_busy && !isManualForce) {
    _loadTimer = setTimeout(function() { _load(); }, 3000);
    return;
  }

  try {
    var r = await _api('/api/status');
    if (!r.ok) throw new Error(r.status);
    _state = await r.json();
    if (_state.language) _lang = _state.language;
    if (_state.theme) _theme(_state.theme);
    _i18nApply();
    _render();
  } catch(e) {
    console.error('load err', e);
  }
  
  _loadTimer = setTimeout(function() { _load(); }, 3000);
}

_load();

_api('/api/status').then(function(r){return r.json();}).then(function(d){
  if(d.url){
    var el=document.getElementById('urlDisplay');if(el)el.textContent=d.url;
    var qr=document.getElementById('qr-container');
    if(qr&&typeof QRCode!=='undefined'){new QRCode(qr,{text:d.url,width:160,height:160,colorDark:d.theme&&d.theme.accent||'#F59E0B',colorLight:d.theme&&d.theme.bgCard||'#1A1A1A'});}
  }
});
</script>
</body>
</html>
""";

    private const string _styleCss = """
:root{--accent:#F59E0B;--bg-dark:#0D0D0D;--bg-card:#1A1A1A;--bg-tertiary:#252525;--text-primary:#FFFFFF;--text-secondary:#888888;--text-muted:#555555;--success:#22C55E;--danger:#EF4444}
*{margin:0;padding:0;box-sizing:border-box}
body{background:var(--bg-dark);color:var(--text-primary);font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,sans-serif;min-height:100vh;line-height:1.5;-webkit-font-smoothing:antialiased}
.container{max-width:860px;margin:0 auto;padding:24px}
header{display:flex;align-items:center;justify-content:space-between;margin-bottom:28px}
.brand{display:flex;align-items:center;gap:12px}
.logo{width:40px;height:40px;background:var(--accent);border-radius:12px;display:flex;align-items:center;justify-content:center;font-size:20px;font-weight:800;color:#fff;flex-shrink:0}
h1{font-size:20px;font-weight:700}
.badge{padding:4px 14px;border-radius:20px;font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:.5px;background:var(--accent);color:#fff;transition:background .3s}
main{display:grid;grid-template-columns:1fr 1fr;gap:16px}
@media(max-width:680px){main{grid-template-columns:1fr}}
.card{background:var(--bg-card);border-radius:14px;padding:20px}
.card-header{display:flex;align-items:center;justify-content:space-between;margin-bottom:14px}
.card-header h2{display:flex;align-items:center;gap:8px;font-size:14px;font-weight:700;text-transform:uppercase;letter-spacing:.3px;color:var(--text-secondary)}
.card-full{grid-column:1/-1}
.actions-list{display:flex;flex-direction:column;gap:2px}
.action-row{display:flex;align-items:center;justify-content:space-between;padding:10px 12px;background:var(--bg-tertiary);border-radius:10px;margin-bottom:4px}
.action-info{display:flex;flex-direction:column;gap:2px}
.action-label{font-size:13px;font-weight:600;color:var(--text-primary)}
.action-desc{font-size:11px;color:var(--text-muted)}
.toggle-btn{min-width:56px;padding:6px 14px;border-radius:8px;border:2px solid var(--text-muted);background:transparent;color:var(--text-muted);font-size:12px;font-weight:700;cursor:pointer;transition:all .2s;text-transform:uppercase;letter-spacing:.5px}
.toggle-btn.active{border-color:var(--accent);background:var(--accent);color:#fff}
.toggle-btn:hover{opacity:.8}
.item{display:flex;align-items:center;gap:10px;padding:10px 12px;background:var(--bg-tertiary);border-radius:10px;margin-bottom:6px;transition:opacity .2s}
.item:last-child{margin-bottom:0}
.item-body{flex:1;min-width:0}
.item-name{display:block;font-size:13px;font-weight:600;color:var(--text-primary);overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.item-pid{font-size:11px;color:var(--text-muted);margin-top:1px}
.item-status{font-size:11px;color:var(--text-secondary);margin-top:1px}
.empty{font-size:13px;color:var(--text-muted);text-align:center;padding:16px 0}
.dot{width:10px;height:10px;border-radius:50%;flex-shrink:0;transition:background .3s}
.dot.online{background:var(--success,#22C55E);box-shadow:0 0 6px var(--success,#22C55E)}
.dot.offline{background:var(--text-muted)}
.btn{display:inline-flex;align-items:center;gap:6px;padding:8px 16px;border-radius:8px;border:none;font-size:12px;font-weight:700;cursor:pointer;transition:all .15s;text-transform:uppercase;letter-spacing:.3px}
.btn-accent{background:var(--accent);color:#fff}
.btn-danger{background:transparent;color:var(--danger);border:1px solid var(--danger)}
.btn-danger:hover{background:var(--danger);color:#fff}
.btn-sm{padding:5px 12px;font-size:11px}
.btn-icon{width:30px;height:30px;padding:0;display:flex;align-items:center;justify-content:center;background:transparent;color:var(--text-muted);border:none;border-radius:8px;cursor:pointer;transition:all .15s;flex-shrink:0}
.btn-icon:hover{background:rgba(255,255,255,.08);color:var(--danger)}
.kill-btn:hover{color:var(--danger)!important}
.kill-btn svg{pointer-events:none}
.wp-cb{width:22px;height:22px;cursor:pointer;accent-color:var(--accent);flex-shrink:0}
.access-content{display:flex;flex-direction:column;align-items:center;gap:12px}
#qr-container{padding:12px 0}
#qr-container img,#qr-container canvas{border-radius:12px}
#urlDisplay{font-size:13px;color:var(--accent);font-weight:600;word-break:break-all;text-align:center}
.hint{font-size:11px;color:var(--text-muted);text-align:center}
.login-box{max-width:360px;margin:100px auto;background:var(--bg-card);padding:40px;border-radius:16px;text-align:center}
.login-box .logo{margin:0 auto 16px}
.login-box h1{font-size:20px;margin-bottom:8px}
.login-box p{font-size:14px;color:var(--text-secondary);margin-bottom:20px}
.login-box input{width:100%;padding:12px;border-radius:8px;border:1px solid var(--text-muted);background:var(--bg-tertiary);color:var(--text-primary);font-size:15px;margin-bottom:16px;outline:none}
.login-box input:focus{border-color:var(--accent)}
.login-box button{width:100%;padding:12px;border-radius:8px;border:none;background:var(--accent);color:#fff;font-size:15px;font-weight:700;cursor:pointer}
.login-box button:hover{opacity:.85}
.error{color:var(--danger);font-size:13px;margin-top:8px}
""";

    private const string _scriptJs = """
async function init(){
  try{
    const r=await fetch('/api/status',{headers:{'Authorization':'Bearer '+localStorage.getItem('token')}});
    if(!r.ok)throw Error();
    const d=await r.json();
    if(d.url){document.getElementById('urlDisplay').textContent=d.url;
      new QRCode(document.getElementById('qr-container'),{text:d.url,width:160,height:160,colorDark:'#22C55E',colorLight:'#0A0A0A'});
    }
  }catch(e){window.location='/login';}
}
document.addEventListener('DOMContentLoaded',init);
""";

    private const string _qrcodeJs = """
var QRCode;!function(){function a(a){this.mode=c.MODE_8BIT_BYTE,this.data=a,this.parsedData=[];for(var b=[],d=0,e=this.data.length;e>d;d++){var f=this.data.charCodeAt(d);f>65536?(b[0]=240|(1835008&f)>>>18,b[1]=128|(258048&f)>>>12,b[2]=128|(4032&f)>>>6,b[3]=128|63&f):f>2048?(b[0]=224|(61440&f)>>>12,b[1]=128|(4032&f)>>>6,b[2]=128|63&f):f>128?(b[0]=192|(1984&f)>>>6,b[1]=128|63&f):b[0]=f,this.parsedData=this.parsedData.concat(b)}this.parsedData.length!=this.data.length&&(this.parsedData.unshift(191),this.parsedData.unshift(187),this.parsedData.unshift(239))}function b(a,b){this.typeNumber=a,this.errorCorrectLevel=b,this.modules=null,this.moduleCount=0,this.dataCache=null,this.dataList=[]}function c(a,b){if(void 0==a.length)throw new Error(a.length+"/"+b);for(var c=0;c<a.length&&0==a[c];)c++;this.num=new Array(a.length-c+b);for(var d=0;d<a.length-c;d++)this.num[d]=a[d+c]}function d(a,b){this.totalCount=a,this.dataCount=b}function e(){this.buffer=[],this.length=0}b.prototype={addData:function(b){var c=new a(b);this.dataList.push(c),this.dataCache=null},isDark:function(a,b){if(0>a||this.moduleCount<=a||0>b||this.moduleCount<=b)throw new Error(a+","+b);return this.modules[a][b]},getModuleCount:function(){return this.moduleCount},make:function(){this._makeImpl(!1,this.getBestMaskPattern())},getBestMaskPattern:function(){for(var a=0,b=0,c=0;8>c;c++){this._makeImpl(!0,c);var d=f(this.getModuleCount());a=((d<<=1)>>>=0)<=a?d:(b=c,d)}return b}},_makeImpl:function(a,c){this.moduleCount=4*this.typeNumber+17,this.modules=new Array(this.moduleCount);for(var d=0;d<this.moduleCount;d++){this.modules[d]=new Array(this.moduleCount);for(var e=0;e<this.moduleCount;e++)this.modules[d][e]=null}this._setupPositionProbePattern(0,0),this._setupPositionProbePattern(this.moduleCount-7,0),this._setupPositionProbePattern(0,this.moduleCount-7),this._setupPositionAdjustPattern(),this._setupTimingPattern(),this._setupTypeInfo(a,c),this.typeNumber>=7&&this._setupTypeNumber(a),null==this.dataCache&&(this.dataCache=b._createData(this.typeNumber,this.errorCorrectLevel,this.dataList)),this._mapData(this.dataCache,c)},_setupPositionProbePattern:function(a,b){for(var c=-1;7>=c;c++)if(!(-1>=a+c||this.moduleCount<=a+c))for(var d=-1;7>=d;d++)-1>=b+d||this.moduleCount<=b+d||(this.modules[a+c][b+d]=c>=0&&6>=c&&(0==d||6==d)||d>=0&&6>=d&&(0==c||6==c)||c>=2&&4>=c&&d>=2&&4>=d?!0:!1)},_getBestMaskPattern:function(){},_setupPositionAdjustPattern:function(){for(var a=f(this.getModuleCount()),b=1;48>b;b++)for(var c=0;48>c;c++){var d=o[b],e=d[0],g=d[1],h=this.moduleCount-3*e;if(!(h<3)){var i=this.moduleCount-3*g;for(var j=0;j<8;j++)for(var k=0;k<8;k++)this.modules[i+k-3][h+j-3]=j>=0&&6>=j&&(0==k||6==k)||k>=0&&6>=k&&(0==j||6==j)||j>=2&&4>=j&&k>=2&&4>=k?!0:!1}}},_setupTimingPattern:function(){for(var a=8;a<this.moduleCount-8;a++)null==this.modules[a][6]&&(this.modules[a][6]=0==a%2);for(var b=8;b<this.moduleCount-8;b++)null==this.modules[6][b]&&(this.modules[6][b]=0==b%2)},_setupTypeNumber:function(a){for(var b=c.getBCHTypeNumber(this.typeNumber),d=0;18>d;d++){var e=!a&&1==(1&b>>d);this.modules[Math.floor(d/3)][d%3+this.moduleCount-8-3]=e}for(var d=0;18>d;d++){var e=!a&&1==(1&b>>d);this.modules[d%3+this.moduleCount-8-3][Math.floor(d/3)]=e}},_setupTypeInfo:function(a,b){for(var c=this.errorCorrectLevel<<3|b,d=n[c],e=0;15>e;e++){var f=!a&&1==(1&d>>e);6>e?this.modules[e][8]=f:8>e?this.modules[e+1][8]=f:this.modules[this.moduleCount-15+e][8]=f}for(var e=0;15>e;e++){var f=!a&&1==(1&d>>e);8>e?this.modules[8][this.moduleCount-e-1]=f:9>e?this.modules[8][15-e-1+1]=f:this.modules[8][15-e-1]=f}this.modules[this.moduleCount-8][8]=1},_mapData:function(a,b){for(var c=-1,d=this.moduleCount-1,e=7,g=0,h=this.moduleCount-1;h>0;h-=2)for(6==h&&h--;;){for(var i=0;2>i;i++)if(null==this.modules[d][h-i]){var j=!1;g<a.length&&(j=1==(1&a[g]>>>e));var k=p.getMask(b,d,h-i);k&&(j=!j),this.modules[d][h-i]=j,e--,-1==e&&(g++,e=7)}if(d+=c,0>d||this.moduleCount<=d){d-=c,c=-c;break}}}},b.PAD0=236,b.PAD1=17,b._createData=function(a,c,d){for(var e=d[0],f=1;f<d.length;f++)e=new c(e,d[f]);for(var g=0,h=0;h<d.length;h++)g+=d[h].mode.dataCount;for(var i=e.getLength(),j=0,k=0,l=0;k<d.length;k++){var m=d[k];j+=m.mode.dataCount,k>0&&(j+=d[k].mode.totalCount-d[k].mode.dataCount)}var n=new Array(j),o=0,p=0;for(var q=0;q<d.length;q++){var r=d[q];for(var s=0;s<r.mode.dataCount;s++)n[o++]=r.mode.data[s];for(;o<r.mode.totalCount;)n[o++]=0}for(var t=0;t<d.length;t++){var u=d[t].mode;for(var v=0;v<u.totalCount;v++)n[o+v]^=u.data[v]}return n},b._createData=function(a,c,d){for(var e=0,f=1;f<d.length;f++){var g=d[f].mode;e=(e<<1)+g.totalCount-g.dataCount}var h=Array(a-1).fill(0);h.unshift(1);var i=new c(h,0),j=0;for(var k=0;k<d.length;k++){var l=d[k].mode;for(var m=0;m<l.totalCount;m++){var n=d[k].mode.data[m];n!==void 0?(i.data[j]=n,i.data[j+1]=0):(i.data[j]=0,i.data[j+1]=n),j+=2}}return i.data},b._makeImpl=function(a,c){this.moduleCount=4*this.typeNumber+17,this.modules=new Array(this.moduleCount);for(var d=0;d<this.moduleCount;d++)this.modules[d]=new Array(this.moduleCount).fill(null);this._setupPositionProbePattern(0,0),this._setupPositionProbePattern(this.moduleCount-7,0),this._setupPositionProbePattern(0,this.moduleCount-7),this._setupPositionAdjustPattern(),this._setupTimingPattern(),this._setupTypeInfo(a,c),this.typeNumber>=7&&this._setupTypeNumber(a),null==this.dataCache&&(this.dataCache=b._createData(this.typeNumber,this.errorCorrectLevel,this.dataList)),this._mapData(this.dataCache,c)},c.prototype={getLength:function(){return this.num.length},getBit:function(a){return 1==(1&this.num[this.num.length-1-a])}};var f="function"==typeof define&&define.amd;var g=function(a,b){return(a+b)*(a+b+1)/2+b};var h=[1,1,1,1,1,1,1,1,1,1,2,2,1,2,1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,2,1,2,1,1,1,1,1,2,1,1,1,1,1,2,1,1,2,1,2,2,1,1,1,1,1,2,1,1,1,1,1,2,2,1,1,2,1,1,1,1,2,1,2,1,1,2,2,1,1,1,1,1,2,1,1,1,2,1,2,1,1,1,2,1,2,1,1,1,2,1,2,2,1,1,1,1,1,1,1,1,2,1,2,1,1,2,1,2,1,1,2,1,2,1,2,2,1,1,1,1,1,1,1,1,1,1,2];var i=[0,1,1,2,1,2,2,3,1,2,2,3,2,3,3,4];var j=[0,3,3,6,3,6,6,9,3,6,6,9,6,9,9,12];var k=[0,2,2,4,2,4,4,6,2,4,4,6,4,6,6,8];var l=[0,2,2,4,2,4,4,6,2,4,4,6,4,6,6,8];var m=function(a,b){var c,d=a,e=b;for(c=0;0!=d||0!=e;)d>>>=1,e>>>=1,c++;return c};var n=function(a,b){var c=0;for(var d=0;d<b.length;d++)if(b[d]==a){c=d;break}return c};var o=function(a,b){return a<<8|b};var p={MASK_000:function(a,b,c){return 0==(a+b+c)%2},MASK_001:function(a,b,c){return 0==a%2},MASK_010:function(a,b,c){return 0==c%3},MASK_011:function(a,b,c){return 0==(a+b)%3},MASK_100:function(a,b,c){return 0==(Math.floor(a/2)+Math.floor(c/3))%2},MASK_101:function(a,b,c){return 0==a*c%2+a*c%3},MASK_110:function(a,b,c){return 0==(a*c%2+a*c%3)%2},MASK_111:function(a,b,c){return 0==(a*c%3+(a+c)%2)%2},getMask:function(a,b,c){return this["MASK_00"+a](b,c)}},q=function(a){return a<0&&(a=0),a>255&&(a=255),a},r=function(a,b,c){return a*(b+c)+b},s=function(a,b,c){return a*(b+c)*(c+b)+b+c};function t(){this.QRCodeLimitLength=[[17,14,11,7],[32,26,20,14],[53,42,32,24],[78,62,46,34],[106,84,60,44],[134,106,74,58],[154,122,86,64],[192,152,108,84],[230,180,130,98],[271,213,151,119],[321,251,177,137],[367,287,203,155],[425,331,241,177],[458,362,258,194],[520,412,292,220],[586,450,322,250],[644,504,364,280],[718,560,394,310],[792,624,442,338],[858,666,482,382],[929,711,509,403],[1003,779,565,439],[1091,857,611,461],[1171,911,661,511],[1273,997,715,535],[1367,1059,751,593],[1465,1125,805,625],[1528,1190,868,658],[1628,1264,908,698],[1732,1370,982,742],[1840,1452,1030,790],[1952,1538,1112,842],[2068,1628,1168,898],[2188,1722,1228,958],[2304,1809,1283,983],[2431,1911,1351,1051],[2563,1989,1423,1093],[2699,2099,1499,1139],[2809,2213,1579,1219],[2953,2331,1663,1273]];this.getLength=function(a,b){return this.QRCodeLimitLength[a-1][b]}}function u(a){this.typeNumber=1,this.errorCorrectLevel="L",this.qrcode=null,this._htOption={width:256,height:256,typeNumber:4,colorDark:"#000000",colorLight:"#ffffff",correctLevel:"H"},this._htOption.text="",a&&(this._htOption=Object.assign({},this._htOption,a)),this._htOption.text&&this.make()}u.prototype.make=function(){this.qrcode&&this.qrcode=null;var a=this._htOption;if(!a.text)return;this.qrcode=new b(a.typeNumber||4,a.correctLevel||"H");this.qrcode.addData(a.text);this.qrcode.make();var c=a.width||256,d=a.height||256,e=document.createElement("canvas");e.width=c,e.height=d;var f=e.getContext("2d"),g=this.qrcode.getModuleCount(),h=c/g,i=d/g;f.fillStyle=a.colorLight||"#ffffff",f.fillRect(0,0,c,d),f.fillStyle=a.colorDark||"#000000";for(var j=0;j<g;j++)for(var k=0;k<g;k++)this.qrcode.isDark(j,k)&&f.fillRect(Math.round(j*h),Math.round(k*i),Math.ceil(h),Math.ceil(i));this._el=e},u.prototype.getCanvas=function(){return this._el},u.prototype.toDataURL=function(){return this._el.toDataURL()},u.prototype.appendChild=function(a){a.appendChild(this._el)},window.QRCode=u}();
""";
}
