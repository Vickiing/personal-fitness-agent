# UX da visualização da ficha A/B

## Decisões

- A ficha é importada apenas após ação explícita do usuário; a planilha é lida localmente e a versão estruturada fica no SQLite privado do aplicativo.
- A seleção A/B é visível e direta. A lista usa CollectionView, sem um ScrollView pai, para aproveitar a virtualização de itens nativa do .NET MAUI.
- Cada cartão mantém nome, ordem, séries, repetições, descanso e prioridade em uma área compacta. Um botão com alvo de pelo menos 48 dp abre os detalhes; nenhuma ação depende apenas de gesto.
- O detalhe apresenta uma ilustração vetorial acessível, quatro passos numerados e uma animação de movimento acionada pelo usuário. A animação não inicia sozinha, não baixa mídia e funciona offline.
- Texto da ficha permanece como conteúdo fornecido pelo usuário. Não é interpretado como instrução para o app. Carga de referência recebe rótulo explícito de anotação, nunca de prescrição.

## Pesquisa consultada

- A documentação do [.NET MAUI CollectionView](https://learn.microsoft.com/dotnet/maui/user-interface/controls/collectionview) recomenda CollectionView para listas roláveis e explica que os controles nativos fazem virtualização.
- As [orientações de performance do .NET MAUI](https://learn.microsoft.com/dotnet/maui/deployment/performance) recomendam imagens próximas ao tamanho de apresentação e evitar elementos desnecessários para reduzir trabalho de decodificação e renderização.
- A [orientação Android sobre layouts e padrões de navegação](https://developer.android.com/design/ui/mobile/guides/layout-and-content/layout-and-nav-patterns) informa que tabs são adequadas para navegação de primeiro nível; A/B permanece como seleção secundária dentro da ficha.
- A [orientação Android para acessibilidade em views](https://developer.android.com/guide/topics/ui/accessibility/views/apps-views) recomenda alvos de toque de 48 dp e rótulos descritivos para controles.

## Validação

O parser XLSX e a persistência local são cobertos por testes com dados sintéticos; o arquivo escolhido pelo usuário foi validado em execução local sem imprimir seu conteúdo. O harness da UI compila os cartões e a tela de detalhes. A inspeção visual no Android/emulador ainda depende de SDK e dispositivo/emulador disponíveis.
