// 将维护用原型合并为可独立复制的单文件 HTML，所有资源均内嵌。
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'..');
const folder=path.join(root,'design/mockups');
const resources=Object.fromEntries(['zh-CN','en-US'].map(language=>[language,JSON.parse(fs.readFileSync(path.join(root,'src/HomeLedger.Core/I18n',language+'.json'),'utf8'))]));
const metadata={version:fs.readFileSync(path.join(root,'Directory.Build.props'),'utf8').match(/<Version>([^<]+)<\/Version>/)[1]};
const resourceFile=path.join(folder,'product-resources.js');
fs.writeFileSync(resourceFile,fs.readFileSync(resourceFile,'utf8').replace(/window\.homeLedgerMetadata=.*;\r?\n/g,'').replace(/window\.homeLedgerResources=.*;\r?\n/,'window.homeLedgerResources='+JSON.stringify(resources)+';\n')+'window.homeLedgerMetadata='+JSON.stringify(metadata)+';\n');
const pages={};
for(const file of fs.readdirSync(folder).filter(name=>name.endsWith('.html'))){
  let html=fs.readFileSync(path.join(folder,file),'utf8').replace('<html lang="zh-CN">','<html lang="zh-CN" data-page="'+file+'">');
  html=html.replace(/<link rel="stylesheet" href="([^"]+)">/g,(_,name)=>'<style>'+fs.readFileSync(path.join(folder,name),'utf8')+'</style>');
  html=html.replace(/<script src="([^"]+)"><\/script>/g,(_,name)=>'<script>'+fs.readFileSync(path.join(folder,name),'utf8').replaceAll('</script','<\\/script')+'</script>');
  html=html.replaceAll('../../logo.png','data:image/png;base64,'+fs.readFileSync(path.join(root,'logo.png')).toString('base64'));
  pages[file]=html;
}
const labels={'login.html':'登录','member-ledger.html':'成员 · 记账','member-deposits.html':'成员 · 存款','member-report.html':'成员 · 报表','admin-overview.html':'管理员 · 总览','admin-details.html':'管理员 · 明细','admin-members.html':'管理员 · 成员','admin-backup.html':'管理员 · 备份','settings.html':'外观与语言','about.html':'关于与更新'};
const script=`const pages=${JSON.stringify(pages).replaceAll('<','\\u003c')};
let state={};window.updatePrototypeState=next=>{state={...state,...next};};window.navigatePrototype=(name,next={})=>{state={...state,...next};const html=pages[name];if(!html)throw Error('Unknown page '+name);document.querySelector('#prototype-page').value=name;document.querySelector('iframe').srcdoc=html.replace('const query = new URLSearchParams(location.search);','const query = new URLSearchParams('+JSON.stringify(new URLSearchParams(state).toString())+');');};
document.querySelector('#prototype-page').onchange=event=>navigatePrototype(event.target.value);navigatePrototype(new URLSearchParams(location.search).get('page')||'member-ledger.html',Object.fromEntries(new URLSearchParams(location.search)));`;
const output=`<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>HomeLedger · 完整交互原型</title><style>html,body{margin:0;background:#ddd;font-family:Segoe UI,Microsoft YaHei,sans-serif}iframe{width:1280px;height:840px;border:0;display:block;margin:0 auto}.prototype-switch{position:fixed;right:105px;top:2px;z-index:100;border:1px solid #ccc;border-radius:5px;background:#fff;padding:2px;font-size:11px;max-width:160px;opacity:0}.prototype-switch:hover,.prototype-switch:focus{opacity:1}</style></head><body><select class="prototype-switch" id="prototype-page" aria-label="原型页面">${Object.entries(labels).map(([file,label])=>'<option value="'+file+'">'+label+'</option>').join('')}</select><iframe title="HomeLedger 交互原型"></iframe><script>${script}</script></body></html>`;
fs.writeFileSync(path.join(root,'design/index.html'),output);console.log('Bundled '+Object.keys(pages).length+' pages into design/index.html');
