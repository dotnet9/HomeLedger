/* 离线原型展示真实入口和状态，模拟更新绝不启动安装器。 */
(() => {
  const resources = window.homeLedgerResources;
  const paletteNames = ['DotNet','Aurora','Ember','Default','Aquatic','Desert','Dusk','NightSky'];
  const query = new URLSearchParams(location.search);
  let state = { palette: query.get('palette') || 'DotNet', mode: query.get('mode') || 'Light', language: query.get('language') || query.get('lang') || 'zh-CN' };
  const text = key => resources[state.language]['HomeLedger.' + key] || key;
  const dictionary = new Map();
  for (const [key,value] of Object.entries(resources['zh-CN'])) if (key.startsWith('HomeLedger.')) dictionary.set(value.trim(), key);
  const extra = {'报表':'MyReports','设置':'AppearanceTitle','图表总览':'ChartsOverview','报表导出':'OpenReports','默认年利率':'DefaultRate','确认':'Save','初始密码':'Password','首次登录请修改密码':'PasswordChangeRequired'};
  for (const [value,key] of Object.entries(extra)) dictionary.set(value, 'HomeLedger.' + key);

  document.querySelectorAll('.titlebar .wbtn').forEach((old,index)=>{const button=document.createElement('button');button.className=old.className;button.textContent=old.textContent;button.type='button';button.dataset.window=['minimize','maximize','close'][index];button.setAttribute('aria-label',['Minimize','Maximize / restore','Close'][index]);button.onclick=()=>{if(index===1)document.body.classList.toggle('maximized');else toast(state.language==='zh-CN'?'原型窗口操作演示':'Prototype window action');};old.replaceWith(button);});
  const page = document.documentElement.dataset.page || location.pathname.split('/').pop();
  const admin = page.startsWith('admin-');
  const routes = admin ? [['🏠','FamilyOverview','admin-overview.html'],['🔎','TransactionDetails','admin-details.html'],['👥','MemberManagement','admin-members.html'],['🗄️','DataBackup','admin-backup.html']] : [['📝','Ledger','member-ledger.html'],['🏦','DepositInterest','member-deposits.html'],['📊','MyReports','member-report.html']];
  const sidebar = document.querySelector('.sidebar');
  if (sidebar) {
    for (const item of sidebar.querySelectorAll('.nav-item')) {
      const label = item.textContent.trim();
      const route = routes.find(row => label.includes(resources['zh-CN']['HomeLedger.' + row[1]])) || (label.includes('报表') ? ['📊','MyReports',admin?'admin-overview.html':'member-report.html'] : null);
      if (route) { item.onclick = event => { event.preventDefault(); navigate(route[2]); }; item.tabIndex = 0; item.onkeydown = event => { if(event.key==='Enter') item.click(); }; }
      else if (label.includes('设置')) item.onclick = () => openAppearance();
    }
    const actions = document.createElement('div'); actions.className = 'product-actions';
    actions.innerHTML = '<button class="btn" data-key="AppearanceTitle" data-action="appearance"></button><button class="btn" data-key="ChangePassword" data-action="password"></button><button class="btn" data-key="SwitchAccount" data-action="logout"></button><button class="btn" data-key="AboutTitle" data-action="about"></button>';
    sidebar.insertBefore(actions, sidebar.querySelector('.user'));
  } else {
    const target = document.querySelector('.titlebar .spacer');
    if (target) target.insertAdjacentHTML('afterend','<button class="btn sm" data-key="AppearanceTitle" data-action="appearance"></button><button class="btn sm" data-key="AboutTitle" data-action="about"></button>');
  }

  const about = document.createElement('div'); about.className = 'overlay'; about.id = 'product-about';
  about.innerHTML = `<section class="modal"><div class="m-head"><span data-key="AboutTitle"></span><span class="x">✕</span></div><div class="m-body"><div class="about-heading"><img src="../../logo.png"><div><h2>HomeLedger</h2><p data-key="AboutDescription"></p></div></div><div class="card"><div class="update-row"><div><strong data-key="UpdateCheck"></strong><p data-key="AboutUpdateHint"></p></div><button class="btn primary" id="update-action"></button></div><div class="update-status" id="update-status"></div><progress class="update-progress" id="update-progress" max="100" value="0" hidden></progress><button class="btn sm" id="update-cancel" data-key="Cancel" hidden></button></div><dl class="about-info" style="margin-top:22px"><dt data-key="AboutVersion"></dt><dd>0.1.8</dd><dt data-key="AboutPlatform"></dt><dd>.NET 10 · Avalonia 12.1.4</dd><dt data-key="AboutCompileTime"></dt><dd id="about-build">2026-10-15 00:00:00</dd><dt data-key="AboutAuthor"></dt><dd data-key="AboutAuthorName"></dd><dt data-key="AboutRepository"></dt><dd>github.com/dotnet9/HomeLedger</dd><dt data-key="AboutCopyright"></dt><dd>Copyright © https://codewf.com</dd></dl></div><div class="m-foot"><button class="btn close-modal" data-key="AboutClose"></button></div></section>`;
  document.body.append(about);
  const appearance = document.createElement('div'); appearance.className = 'overlay'; appearance.id = 'product-appearance';
  appearance.innerHTML = `<section class="modal" style="width:460px"><div class="m-head"><span data-key="AppearanceTitle"></span><span class="x">✕</span></div><div class="m-body"><div class="settings-grid"><label data-key="AppearanceMode"></label><select id="appearance-mode">${['System','Light','Dark'].map(key=>`<option value="${key}" data-key="Appearance${key}"></option>`).join('')}</select><label data-key="AppearancePalette"></label><select id="appearance-palette">${paletteNames.map(key=>`<option value="${key}" data-key="Appearance${key}"></option>`).join('')}</select><label data-key="AppearanceLanguage"></label><select id="appearance-language"><option value="zh-CN">简体中文</option><option value="en-US">English</option></select></div></div><div class="m-foot"><button class="btn close-modal" data-key="AboutClose"></button></div></section>`;
  document.body.append(appearance);
  function openAppearance() { appearance.classList.add('open'); }
  let updateState = 'idle', timer, failedStage='check', downloadFailed=false, installFailed=false;
  const noAsset=()=>['noasset','checksum-missing'].includes(query.get('scenario'));
  function updateView() {
    const action = document.querySelector('#update-action'), status = document.querySelector('#update-status');
    const actions = {idle:'UpdateCheck',checking:'AboutChecking',available:noAsset()?'UpdateReleasePage':'UpdateDownload',downloading:'AboutDownloading',downloaded:'UpdateInstall',installing:'AboutInstalling',installed:'UpdateCheck',failed:'AboutRetry',canceled:'UpdateCheck'};
    const statuses = {idle:'',checking:'AboutChecking',available:'AboutAvailable',downloading:'AboutDownloadProgress',downloaded:'AboutDownloaded',installed:'AboutInstalled',failed:'AboutFailed',canceled:'AboutCanceled'};
    action.textContent = text(actions[updateState]); action.disabled = ['checking','downloading','installing'].includes(updateState);
    status.textContent = statuses[updateState] ? text(statuses[updateState]).replace('{0}', updateState==='available'?'v0.1.8':updateState==='failed'?'offline':'50%') : '';
    document.querySelector('#update-progress').hidden = updateState !== 'downloading';
    document.querySelector('#update-cancel').hidden = !['checking','downloading'].includes(updateState);
  }
  document.querySelector('#update-action').onclick = () => {
    if(updateState === 'available' && noAsset()) return toast(text('UpdateReleasePage'));
    if(updateState==='available' || updateState==='failed' && failedStage==='download') beginDownload();
    else if(updateState==='downloaded' || updateState==='failed' && failedStage==='install') beginInstall();
    else { failedStage='check';updateState='checking';updateView();timer=setTimeout(()=>{updateState=query.get('scenario')==='offline'?'failed':'available';updateView();},400); }
  };
  function beginDownload(){failedStage='download';updateState='downloading';document.querySelector('#update-progress').value=50;updateView();timer=setTimeout(()=>{const fail=['download-failed','checksum-mismatch'].includes(query.get('scenario'))&&!downloadFailed;downloadFailed=true;updateState=fail?'failed':'downloaded';updateView();},600);}
  function beginInstall(){failedStage='install';updateState='installing';updateView();timer=setTimeout(()=>{const fail=query.get('scenario')==='install-failed'&&!installFailed;installFailed=true;updateState=fail?'failed':'installed';updateView();},300);}
  document.querySelector('#update-cancel').onclick = () => { clearTimeout(timer); updateState='canceled'; updateView(); };
  document.querySelector('#appearance-mode').onchange = event => { state.mode=event.target.value; apply(); };
  document.querySelector('#appearance-palette').onchange = event => { state.palette=event.target.value; apply(); };
  document.querySelector('#appearance-language').onchange = event => { state.language=event.target.value; apply(); };
  document.querySelectorAll('.overlay .x,.overlay .close-modal,.overlay .m-foot .btn:not(.primary)').forEach(button=>button.onclick=()=>button.closest('.overlay').classList.remove('open'));
  document.querySelectorAll('[data-action]').forEach(button=>button.onclick=()=>{
    if(button.dataset.action==='appearance') openAppearance();
    else if(button.dataset.action==='about') about.classList.add('open');
    else if(button.dataset.action==='logout') navigate('login.html');
    else changePassword();
  });
  document.querySelectorAll('.btn:not([data-action])').forEach(button=>{
    if(button.closest('#product-about,#product-appearance') || button.onclick) return;
    button.onclick=()=>{
      const label=button.textContent.trim();
      const existing=[...document.querySelectorAll('.overlay:not(#product-about):not(#product-appearance)')];
      if((label.includes('新建')||label.includes('结算')||label.includes('Create')||label.includes('Settle')) && existing.length) existing[0].classList.add('open');
      else toast(label+' ✓');
    };
  });
  document.querySelectorAll('.input').forEach(input=>{input.contentEditable='true';input.setAttribute('role','textbox');});
  document.querySelectorAll('.pager .pg').forEach(button=>button.onclick=()=>{document.querySelectorAll('.pager .pg').forEach(item=>item.classList.remove('on'));button.classList.add('on');});
  function showForm(title, fields, save) {
    document.querySelector('#prototype-form')?.remove();
    const overlay=document.createElement('div');overlay.className='overlay open';overlay.id='prototype-form';
    overlay.innerHTML='<form class="modal"><div class="m-head"><strong></strong><button type="button" class="btn close-modal">✕</button></div><div class="m-body"></div><div class="m-foot"><button type="button" class="btn close-modal"></button><button class="btn primary" type="submit"></button></div></form>';
    overlay.querySelector('strong').textContent=text(title);
    const body=overlay.querySelector('.m-body');
    fields.forEach(field=>{const label=document.createElement('label');label.className='prototype-field';label.textContent=text(field.key);const input=document.createElement('input');input.name=field.key;input.type=field.type||'text';input.value=field.value??'';input.required=field.key!=='Note';if(input.type==='number'){input.min=field.min??'0.01';input.step='0.01';}label.append(input);body.append(label);});
    const cancel=overlay.querySelector('.m-foot .close-modal');cancel.textContent=text('Cancel');
    overlay.querySelector('[type=submit]').textContent=text('Save');
    overlay.querySelectorAll('.close-modal').forEach(button=>button.onclick=()=>overlay.remove());
    overlay.querySelector('form').onsubmit=event=>{event.preventDefault();if(save([...body.querySelectorAll('input')].map(input=>input.value))!==false)overlay.remove();};
    document.body.append(overlay);body.querySelector('input')?.focus();
  }
  function changePassword(){showForm('ChangePassword',[{key:'NewPassword',type:'password'},{key:'ConfirmPassword',type:'password'}],values=>{if(values[0].length<6){toast(text('PasswordTooShort'));return false;}if(values[0]!==values[1]){toast(text('PasswordMismatch'));return false;}toast(text('ConfirmChange')+' ✓');return true;});}
  const number=value=>Number(value.replace(/[^0-9.-]/g,''))||0;
  const today='2026-08-21';
  const mainButtons=[...document.querySelectorAll('.main .btn')].filter(button=>!button.closest('.overlay'));
  const findButton=label=>mainButtons.find(button=>button.textContent.includes(label));
  const cloneRow=(table,values)=>{const row=table.querySelector('tbody tr').cloneNode(true);values.forEach((value,index)=>{if(value!==null)row.cells[index].textContent=value;});table.querySelector('tbody').prepend(row);return row;};
  document.querySelectorAll('.tabs span').forEach(tab=>{if(tab.textContent.includes('明细'))tab.onclick=()=>navigate('admin-details.html');else tab.onclick=()=>navigate('admin-overview.html');});
  if(page==='login.html'){
    const button=document.querySelector('.login-card .btn.primary');button.onclick=()=>navigate(document.querySelector('.login-card .input').textContent.trim().startsWith('admin')?'admin-overview.html':'member-ledger.html');
    document.querySelector('.login-card').onkeydown=event=>{if(event.key==='Enter')button.click();};
  }
  if(page==='member-ledger.html'){
    const table=document.querySelector('.records table');
    const bindRow=row=>{const buttons=row.querySelectorAll('.btn');buttons[0].onclick=()=>showForm('EditTransaction',[{key:'Amount',type:'number',value:Math.abs(number(row.cells[3].textContent))},{key:'Date',type:'date',value:row.cells[0].textContent},{key:'Note',value:row.cells[4].textContent}],values=>{row.cells[0].textContent=values[1];row.cells[3].textContent=(row.cells[3].textContent.startsWith('-')?'-':'+')+Number(values[0]).toFixed(2);row.cells[4].textContent=values[2];});buttons[1].onclick=()=>row.remove();};
    table.querySelectorAll('tbody tr').forEach(bindRow);
    document.querySelectorAll('.seg span').forEach(option=>option.onclick=()=>{option.parentElement.querySelectorAll('span').forEach(item=>item.classList.remove('on'));option.classList.add('on');});
    const quick=document.querySelector('.quick');
    const save=()=>{const amount=number(quick.querySelector('.input.big').textContent);if(amount<=0)return toast(text('PositiveAmountRequired'));const expense=quick.querySelector('.seg span.on')===quick.querySelector('.seg span');const row=cloneRow(table,[today,expense?text('Expense'):text('Income'),quick.querySelector('.select').textContent.replace(' ▾',''),(expense?'-':'+')+amount.toFixed(2),quick.querySelector('.field:nth-child(5) .input').textContent,'']);row.cells[5].innerHTML='<button class="btn link sm">'+text('Edit')+'</button><button class="btn link sm">'+text('Delete')+'</button>';bindRow(row);quick.querySelector('.input.big').textContent='';toast(text('TransactionSaved'));};
    quick.querySelector('.btn.primary').onclick=save;quick.onkeydown=event=>{if(event.key==='Enter'){event.preventDefault();save();}};
    const search=document.querySelector('.records .toolbar .input');search.textContent='';search.dataset.placeholder=text('SearchNotes');search.oninput=()=>table.querySelectorAll('tbody tr').forEach(row=>row.hidden=!row.cells[4].textContent.toLowerCase().includes(search.textContent.toLowerCase()));
  }
  if(page==='member-deposits.html'){
    const table=document.querySelector('.main table');
    const selected=()=>[...table.querySelectorAll('tbody tr')].filter(row=>row.querySelector('input:checked'));
    table.querySelector('thead input').onchange=event=>table.querySelectorAll('tbody input').forEach(input=>input.checked=event.target.checked);
    findButton('存入').onclick=()=>showForm('AddDeposit',[{key:'Amount',type:'number'},{key:'AnnualRateHint',type:'number',value:'10',min:'0'},{key:'DepositDate',type:'date',value:today},{key:'Note'}],values=>{const amount=Number(values[0]);const row=cloneRow(table,[null,amount.toFixed(2),values[1]+'%',values[2],'+0.00',amount.toFixed(2),text('Deposit')+' '+amount.toFixed(2),text('Accruing')]);row.querySelector('input').checked=true;});
    findButton('支取').onclick=()=>{const row=selected()[0];if(!row)return toast(text('SelectDeposits'));showForm('WithdrawDeposit',[{key:'WithdrawalAmount',type:'number'},{key:'WithdrawalDate',type:'date',value:today},{key:'Note'}],values=>{const available=number(row.cells[5].textContent);if(Number(values[0])>available)return false;const remaining=available-Number(values[0]);if(remaining>0){const next=cloneRow(table,[null,remaining.toFixed(2),null,values[1],'+0.00',remaining.toFixed(2),text('WithdrawalRemainder'),text('Accruing')]);next.querySelector('input').checked=true;}row.cells[0].textContent='';row.cells[5].textContent=text('Closed');row.cells[7].textContent=text('Closed');row.cells[6].textContent+=' → '+text('Withdraw')+' '+values[0];});};
    findButton('结算').onclick=()=>{const rows=selected();if(!rows.length)return toast(text('SelectDeposits'));showForm('SettleConfirmTitle',[{key:'SettlementDate',type:'date',value:today}],values=>{let total=0;rows.forEach(row=>{const interest=number(row.cells[4].textContent);total+=interest;row.cells[1].textContent=number(row.cells[5].textContent).toFixed(2);row.cells[3].textContent=values[0];row.cells[4].textContent='+0.00';row.cells[6].textContent+=' → '+text('SettleInterest')+' '+interest.toFixed(2);});cloneRow(document.querySelector('.sub-table'),[values[0],String(rows.length),'+'+total.toFixed(2),'Alex']);});};
  }
  if(page==='admin-members.html'){
    const table=document.querySelector('.main table');
    const bindRow=row=>row.querySelectorAll('.btn').forEach(button=>{const label=button.textContent;button.onclick=()=>{if(label.includes('停用')||label.includes('启用')){const disabled=row.dataset.disabled==='true';row.dataset.disabled=String(!disabled);row.cells[3].textContent=text(disabled?'Enable':'Disabled');button.textContent=text(disabled?'Disable':'Enable');}else if(label.includes('重置'))toast(text('ResetPassword')+' ✓');else changePassword();};});
    table.querySelectorAll('tbody tr').forEach(row=>{row.dataset.disabled=String(row.cells[3].textContent.includes('停用'));bindRow(row);});
    findButton('新建').onclick=()=>showForm('CreateMember',[{key:'Username'},{key:'DisplayName'},{key:'Password',type:'password'}],values=>{if(values[0].length<2||values[2].length<6)return false;const row=cloneRow(table,[values[0],values[1],text('Member'),text('Enable'),'0',today,null]);row.cells[6].innerHTML='<button class="btn link sm">重置密码</button><button class="btn link sm">停用</button>';bindRow(row);});
    const search=document.querySelector('.toolbar .input');search.textContent='';search.dataset.placeholder=text('Username')+' / '+text('DisplayName');search.oninput=()=>table.querySelectorAll('tbody tr').forEach(row=>row.hidden=!(row.cells[0].textContent+row.cells[1].textContent).toLowerCase().includes(search.textContent.toLowerCase()));
  }
  mainButtons.filter(button=>/CSV|导出当前/.test(button.textContent)).forEach(button=>button.onclick=()=>{const rows=[...document.querySelectorAll('.main table tr')].map(row=>[...row.cells].map(cell=>'"'+cell.textContent.replaceAll('"','""')+'"').join(','));const link=document.createElement('a');link.download='HomeLedger-prototype.csv';link.href=URL.createObjectURL(new Blob(['\ufeff'+rows.join('\r\n')],{type:'text/csv;charset=utf-8'}));link.click();URL.revokeObjectURL(link.href);});
  function toast(value) { document.querySelector('.prototype-toast')?.remove();const box=document.createElement('div');box.className='prototype-toast';box.textContent=value;document.body.append(box);setTimeout(()=>box.remove(),1800); }
  function navigate(file) {
    if(window.parent !== window && window.parent.navigatePrototype) window.parent.navigatePrototype(file, state);
    else location.href=file+'?lang='+state.language+'&palette='+state.palette+'&mode='+state.mode;
  }
  function apply() {
    if(window.parent !== window && window.parent.updatePrototypeState) window.parent.updatePrototypeState(state);
    const dark = state.mode === 'Dark' || state.mode === 'System' && matchMedia('(prefers-color-scheme: dark)').matches;
    const palette = window.homeLedgerPalettes[state.palette + (dark?'Dark':'Light')] || window.homeLedgerPalettes.DotNetLight;
    for(const [key,value] of Object.entries(palette)) document.documentElement.style.setProperty(key,value);
    document.documentElement.lang=state.language;
    document.querySelectorAll('[data-key]').forEach(element=>element.textContent=text(element.dataset.key));
    const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);
    let node; const nodes=[];while(node=walker.nextNode()) nodes.push(node);
    for(const current of nodes) {
      if(current.parentElement.closest('script,style,[data-key]'))continue;
      const original=current._original ?? current.textContent;
      current._original=original;
      const key=dictionary.get(original.trim());
      if(key) current.textContent=state.language==='zh-CN'?original:resources['en-US'][key];
    }
    document.querySelector('#appearance-mode').value=state.mode;
    document.querySelector('#appearance-palette').value=state.palette;
    document.querySelector('#appearance-language').value=state.language;
    updateView();
  }
  apply();
  if(page==='about.html') about.classList.add('open');
  if(page==='settings.html') appearance.classList.add('open');
})();
