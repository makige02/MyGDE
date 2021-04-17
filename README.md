# 使い方

## GoogleSpreadSheet側の設定

ファイル > ウェブに公開　するとURLが生成される

```
https://docs.google.com/spreadsheets/d/e/{Document ID}/pubhtml
```

このDocument IDと、各シートのURLにあるgidが必要

## Unity側の設定

ファイル一式をAssets配下にコピーする

メニューバーに以下が追加されるので実行する

`Tools > MyGDE > Create Settings File`

実行すると設定ファイル生成される

`MasterData / MyGDE Settings`

設定ファイルに必要な情報を追加する

Document ID　ウェブ公開時に生成されたID

Sheet Infos　シート名とgid

最後にImportCSVボタンを押す
