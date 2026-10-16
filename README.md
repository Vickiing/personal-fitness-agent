# Personal Fitness Agent

Aplicativo pessoal de acompanhamento de treinos, alimentação, recuperação e evolução física, com registro por voz e recomendações explicáveis.

> **Status:** specification-first / documentação inicial. Nenhuma funcionalidade está implementada.

## Princípios
- **Offline-first:** registrar e consultar dados essenciais no Android mesmo sem rede.
- **Núcleo determinístico:** progressão, estimativas, regras de restrições e análises devem ser testáveis, sem dependência de LLM.
- **Voz desacoplada:** provedores de Speech-to-Text intercambiáveis; interpretação por gramática/regras no MVP.
- **Privacidade por padrão:** dados pessoais de saúde permanecem fora deste repositório público e de logs de desenvolvimento.
- **Segurança:** o sistema não diagnostica, não prescreve reabilitação e nunca altera automaticamente exercícios com restrições.
- **Baixo custo:** processamento de voz sob demanda, limites de uso e nenhuma VM obrigatória 24/7.

## Documentação
- [Contexto e escopo](docs/PROJECT_CONTEXT.md)
- [Requisitos e critérios de aceite](docs/REQUIREMENTS.md)
- [Arquitetura](docs/ARCHITECTURE.md)
- [Modelo de domínio](docs/DOMAIN_MODEL.md)
- [Fluxo de voz](docs/VOICE_PIPELINE.md)
- [Motor de restrições](docs/RESTRICTION_ENGINE.md)
- [Análises e estimativas](docs/ANALYTICS.md)
- [Segurança, privacidade e custos](docs/SECURITY_AND_COST.md)
- [Backlog e sequenciamento](docs/BACKLOG.md)
- [Instruções dos agentes](AGENTS.md)
- [Registros de decisão](docs/adr/README.md)

## Proposta técnica
Android (.NET MAUI) + SQLite local + Core/Analytics em C# + API Azure Functions para transcrição em nuvem sem expor credenciais. Azure Speech e outro provedor de STT serão avaliados empiricamente. LLM não é exigido no MVP. Os serviços cloud só são acionados quando necessário.

## Próximos passos
Consultar os work items em **Issues** e seguir a ordem de dependências indicada em `docs/BACKLOG.md`. Para executar tarefas, os agentes devem ler `AGENTS.md` antes de modificar código.

## Aviso
As funcionalidades de treino e alimentação são ferramentas de registro e apoio à decisão, não substituem avaliação individual de profissionais de saúde. Não coloque dados pessoais reais em issues, fixtures ou commits públicos.

Licença: a definir pelo mantenedor.