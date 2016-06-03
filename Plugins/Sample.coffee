Title: "CoffeeScript Sample"
Author: "bawNg"
Version: V(0, 1, 0)

Init: ->
  print "JavaScript: Init"

OnServerInitialized: ->
  print "JavaScript: OnServerInitialized"
  command.AddConsoleCommand "server.jshi", @Plugin, "sayhi"

LoadDefaultConfig: ->
  print "JavaScript: LoadDefaultConfig"
  @Config.authLevel = 1
  @Config.Data = ["blubb1", "blubb2"]
  @Config.Extra = oink: "moep"

RequestResult: (code, response) =>
  print "#{@Title} | #{code} | #{response}"

sayhi: ->
  print "JavaScript: sayhi"
  print globalVar
  global = importNamespace ""
  print global.BasePlayer.activePlayerList.Count
  url = "http://loripsum.net/generate.php?p=1&l=short"
  webrequests.EnqueueGet url, RequestResult, @Plugin
  webrequests.EnqueueGet url, ((code, response) -> print "#{code} | #{response}"), @Plugin
  webrequests.EnqueueGet url, ((code, response) => print "#{@Title} | #{code} | #{response}"), @Plugin
  dataObj = data.GetData "jstest"
  dataObj.oink = "walla"
  dataObj.moep = [{}, "oink", 12345]
  data.SaveData "jstest"
  print "hi from js!" + dataObj.oink
  Rust = importNamespace "Rust"
  obj = new Rust.DamageTypeList()
  try
    print "total: " + obj.Total()
    print "total: " + obj.total()
  catch e
    print e.message.toString()
  print Object.keys(obj).join()
