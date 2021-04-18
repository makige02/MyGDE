# 使い方

## GoogleSpreadSheet側の設定

ファイル > ウェブに公開　するとURLが生成される

```
https://docs.google.com/spreadsheets/d/e/{Document ID}/pubhtml
```

このDocument IDと、各シートのURLにあるgidが必要

## Unity側の設定

※Unitask必須なので事前に入れておく

ファイル一式をAssets配下にコピーする

メニューバーに以下が追加されるので実行する

`Tools > MyGDE > Create Settings File`

実行すると設定ファイル生成される

`MasterData / MyGDE Settings`

設定ファイルに必要な情報を追加する

Document ID　ウェブ公開時に生成されたID

Sheet Infos　シート名とgid

最後にImportCSVボタンを押す

## DB作成方法

・使い方はほぼGDE

・1行目が変数名、2行目が型名

・1列目の1行目はkeyで２行目はstring固定

・1行目に小文字でignoreと入れるか、空白にすればその列は無視される

・対応している型は、int、float、string、list_int、list_float、list_string、boolのみ

・list_stringはダブルクォーテーション不要　例`apple,orange,banana`
