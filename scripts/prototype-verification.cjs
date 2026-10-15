// 使用无界面浏览器验证离线原型，不打开系统浏览器。
const {chromium}=require('playwright');
const fs=require('node:fs');
const path=require('node:path');
const {pathToFileURL}=require('node:url');
(async()=>{
  const output=path.resolve(process.argv[2]||'artifacts/prototype-verification');fs.mkdirSync(output,{recursive:true});
  const browser=await chromium.launch({headless:true});
  try {
  const page=await browser.newPage({viewport:{width:1280,height:840},deviceScaleFactor:1});
  const base=path.resolve(__dirname,'../design/mockups');
  const files=fs.readdirSync(base).filter(name=>name.endsWith('.html'));
  const results=[];
  const standalone=path.join(output,'standalone-copy');fs.mkdirSync(standalone,{recursive:true});
  fs.copyFileSync(path.resolve(__dirname,'../design/index.html'),path.join(standalone,'index.html'));
  if(fs.readdirSync(standalone).length!==1)throw Error('Standalone folder must contain only index.html');
  const errors=[], external=[];
  page.on('pageerror',error=>errors.push(error.message));
  const recordRequest=request=>{if(!request.url().startsWith('data:') && request.url()!==pathToFileURL(path.join(standalone,'index.html')).href)external.push(request.url());};
  page.on('request',recordRequest);
  await page.goto(pathToFileURL(path.join(standalone,'index.html')).href);
  const frame=()=>page.frames().find(item=>item.parentFrame());
  for(const file of files){
    await page.selectOption('#prototype-page',file);
    await page.frameLocator('iframe').locator('body').waitFor();
    await frame().waitForFunction(file=>document.documentElement?.dataset.page===file,file);
    await frame().waitForFunction(()=>document.documentElement.style.getPropertyValue('--jade'));
    if(await frame().locator('.titlebar [data-title-palette]').count()!==8||await frame().locator('#title-mode').count()!==1)throw Error('Title appearance controls missing: '+file);
    if(await frame().locator('.titlebar button[data-window]').count()!==3)throw Error('Desktop caption buttons missing: '+file);
    if(!await frame().locator('body').evaluate(el=>parseFloat(getComputedStyle(el).borderTopWidth)>=1))throw Error('Desktop window frame missing: '+file);
    if(await frame().locator('.titlebar').evaluate(el=>el.getBoundingClientRect().height)!==32)throw Error('Desktop title bar height changed: '+file);
    await frame().addStyleTag({content:'*,*::before,*::after{transition:none!important;animation:none!important}'});
    await page.screenshot({path:path.join(output,'standalone-'+file.replace('.html','.png'))});
    results.push('Standalone '+file+': inline resources and route');
  }
  if(!await frame().locator('.studio-title-controls').evaluate(el=>el.getBoundingClientRect().right<=document.querySelector('.titlebar .wbtn').getBoundingClientRect().left))throw Error('Title controls overlap caption buttons');
  for(let i=0;i<3;i++)await frame().locator('#title-mode').click();
  await frame().locator('[data-title-palette=Aurora]').click();
  if(await frame().locator('[data-title-palette=Aurora]').getAttribute('aria-pressed')!=='true')throw Error('Title palette did not apply');
  await frame().locator('#title-language summary').click();await frame().locator('[data-title-language=en-US]').click();
  if(await frame().locator('html').getAttribute('lang')!=='en-US')throw Error('Title language menu did not apply');
  await page.selectOption('#prototype-page','settings.html');
  await page.frameLocator('iframe').locator('#appearance-language').selectOption('en-US');
  await page.frameLocator('iframe').locator('#appearance-palette').selectOption('Ember');
  await page.frameLocator('iframe').locator('#appearance-mode').selectOption('Dark');
  await page.selectOption('#prototype-page','member-ledger.html');
  await frame().waitForFunction(()=>document.documentElement.lang==='en-US');
  if(await frame().locator('#appearance-palette').inputValue()!=='Ember' || await frame().locator('#appearance-mode').inputValue()!=='Dark')throw Error('Standalone theme state lost during navigation');
  await frame().locator('.nav-item').filter({hasText:'Deposit'}).click();
  await frame().waitForFunction(()=>document.documentElement?.dataset.page==='member-deposits.html');
  await frame().locator('[data-action=about]').click();
  if(await frame().locator('#about-build').textContent()!=='2026-10-15 00:00:00')throw Error('About must use the fixed prototype timestamp');
  await frame().locator('#update-action').click();
  await frame().locator('#update-cancel').click();
  if(!(await frame().locator('#update-status').innerText()).toLowerCase().includes('canceled'))throw Error('Standalone cancellation failed');
  await frame().locator('#product-about .close-modal').click();
  await frame().locator('[data-action=logout]').click();
  await frame().waitForFunction(()=>document.documentElement?.dataset.page==='login.html');
  await frame().locator('.login-card .input').first().fill('member');
  await frame().locator('.login-card .btn.primary').click();
  await frame().waitForFunction(()=>document.documentElement?.dataset.page==='member-ledger.html');
  const recordCount=await frame().locator('.records tbody tr').count();
  await frame().locator('.quick .input.big').fill('65.25');
  await frame().locator('.quick .field:nth-child(5) .input').fill('Offline interaction');
  await frame().locator('.quick .btn.primary').click();
  if(await frame().locator('.records tbody tr').count()!==recordCount+1)throw Error('Standalone quick-entry did not add a row');
  await frame().locator('.records tbody tr').first().locator('.btn').first().click();
  await frame().locator('#prototype-form [name=Amount]').fill('75.25');
  await frame().locator('#prototype-form [type=submit]').click();
  if(!(await frame().locator('.records tbody tr').first().innerText()).includes('75.25'))throw Error('Standalone edit did not update the amount');
  await frame().locator('.records tbody tr').first().locator('.btn').last().click();
  if(await frame().locator('.records tbody tr').count()!==recordCount)throw Error('Standalone delete did not remove the row');
  await frame().locator('.nav-item').filter({hasText:'Deposit'}).click();
  await frame().waitForFunction(()=>document.documentElement?.dataset.page==='member-deposits.html');
  await frame().locator('.main table thead input').first().uncheck();
  await frame().locator('.main table tbody input').first().check();
  const historyCount=await frame().locator('.sub-table tbody tr').count();
  await frame().locator('.toolbar .btn.primary').click();
  await frame().locator('#prototype-form [type=submit]').click();
  if(await frame().locator('.sub-table tbody tr').count()!==historyCount+1)throw Error('Standalone settlement did not add history');
  results.push('Standalone login/logout, quick-entry, edit/delete and selected deposit settlement change visible data');
  if(errors.length || external.length)throw Error('Standalone dependencies/errors: '+JSON.stringify({errors,external}));
  page.off('request',recordRequest);
  results.push('Standalone preserves language/palette/mode across selector and sidebar navigation; update cancel works; zero external requests/errors');
  for(const file of files){
    await page.goto(pathToFileURL(path.join(base,file)).href);await page.addStyleTag({content:'*,*::before,*::after{transition:none!important;animation:none!important}'});await page.screenshot({path:path.join(output,file.replace('.html','.png'))});
    results.push(file+': 1280×840 snapshot');
  }
  await page.goto(pathToFileURL(path.join(base,'member-ledger.html')).href);
  await page.locator('[data-action=appearance]').click();
  if(await page.locator('#appearance-palette option').count()!==8)throw Error('Expected eight palettes');
  if(await page.locator('#appearance-mode option').count()!==3)throw Error('Expected three modes');
  for(const palette of ['DotNet','Aurora','Ember','Default','Aquatic','Desert','Dusk','NightSky']){
    await page.selectOption('#appearance-palette',palette);
    for(const mode of ['Light','Dark']){
      await page.selectOption('#appearance-mode',mode);await page.screenshot({path:path.join(output,`appearance-${palette}-${mode}.png`)});
    }
  }
  await page.selectOption('#appearance-language','en-US');
  if(!await page.locator('#product-appearance').innerText().then(text=>text.includes('Language')))throw Error('Language switch failed');
  await page.locator('#product-appearance .close-modal').click();
  await page.locator('[data-action=about]').click();
  await page.locator('#update-action').click();await page.waitForTimeout(500);
  if(!await page.locator('#update-action').innerText().then(text=>text==='Download update'))throw Error('Update availability missing');
  await page.locator('#update-action').click();await page.waitForTimeout(700);
  if(!await page.locator('#update-action').innerText().then(text=>text==='Install update'))throw Error('Verified download state missing');
  await page.locator('#update-action').click();await page.waitForTimeout(350);await page.screenshot({path:path.join(output,'about-installed-en-US.png')});
  for(const scenario of ['offline','noasset','download-failed']){
    await page.goto(pathToFileURL(path.join(base,'about.html')).href+'?lang=en-US&scenario='+scenario);
    await page.locator('#update-action').click();await page.waitForTimeout(500);
    if(scenario==='download-failed'){await page.locator('#update-action').click();await page.waitForTimeout(700);}
    await page.screenshot({path:path.join(output,'about-'+scenario+'.png')});results.push('Update scenario: '+scenario);
  }
  if(errors.length)throw Error('JavaScript errors: '+errors.join('; '));
  fs.writeFileSync(path.join(output,'assertions.txt'),results.join('\n'));console.log('Prototype verification passed: '+results.length+' page/scenario checks; 16 theme snapshots; standalone HTML passed');
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
