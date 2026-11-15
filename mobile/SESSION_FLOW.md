# Registro de treinos

## Sessões

As sessões são persistidas no SQLite local como registros `session`. A ficha continua sendo somente leitura; o app cria um snapshot da ficha ao iniciar uma sessão e usa IDs estáveis para exercícios importados.

O contrato atual é a versão 6. Os campos opcionais adicionados para continuidade são:

- `activeExerciseIndex`: exercício selecionado, iniciando em zero.
- `pausedUtc`: instante UTC em que a sessão foi pausada, ou nulo quando ativa.
- `totalPausedSeconds`: pausas concluídas acumuladas.

Registros anteriores que não têm esses campos são lidos com os valores padrão. A carga, repetições, RIR e observações continuam sendo informados manualmente e armazenados em cada série. Corrigir ou remover uma série cria uma revisão auditada do registro.

## Pausa e retomada

Sair da tela pausa a sessão em andamento. O botão de retomada abre a mesma sessão e restaura o exercício atual e as séries gravadas. A duração registrada exclui períodos de pausa. Retomar ou iniciar avalia novamente as restrições; bloqueios e revisões necessárias impedem a ação, e avisos exigem confirmação.

O cronômetro de descanso é local à tela e usa um prazo UTC para atualizar a contagem. Sair interrompe o cronômetro; ao retomar a sessão, a pessoa pode iniciar outro descanso.

## Privacidade e mídia

Check-ins, restrições, sessões e backups permanecem locais até que um adaptador de sincronização seja explicitamente configurado. A área Privacidade e dados concentra exportação, importação e exclusão local.

Os movimentos sem arquivo licenciado exibem “sem vídeo”. Não há chamadas de mídia em rede nem GIFs de terceiros empacotados nesta versão.
