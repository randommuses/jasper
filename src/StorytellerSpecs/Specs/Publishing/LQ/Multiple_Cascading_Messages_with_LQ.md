# Multiple Cascading Messages with LQ

-> id = 75de41e9-02d0-41d0-be5f-df8d67f1a765
-> lifecycle = Regression
-> max-retries = 0
-> last-updated = 2017-04-05T18:17:44.4439464Z
-> tags = 

[SendMessage]
|> IfTheApplicationIs
    [ServiceBusApplication]
    |> SendMessage messageType=Message1
    ``` channel
    lq.tcp://localhost:2201/one
    ```

    |> SendMessage messageType=Message2
    ``` channel
    lq.tcp://localhost:2201/two
    ```

    |> SendMessage messageType=Message3
    ``` channel
    lq.tcp://localhost:2201/three
    ```

    |> SendMessage messageType=Message4
    ``` channel
    lq.tcp://localhost:2201/four
    ```

    |> ReceivingMessage2CascadesMultiples

|> SendMessage messageType=Message2, name=Tamba Hali
|> TheMessagesSentShouldBe
    [rows]
    |ReceivedAt                   |MessageType|Name      |
    |lq.tcp://localhost:2201/two  |Message2   |Tamba Hali|
    |lq.tcp://localhost:2201/three|Message3   |Tamba Hali|
    |lq.tcp://localhost:2201/four |Message4   |Tamba Hali|

~~~
