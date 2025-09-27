# LLM-Integration-Unity

A small project mad in Unity Engine with a chatbot integrated with an LLM

🔑 Configuração da API Key

Este projeto utiliza a API da OpenAI, que requer uma chave de autenticação (OPENAI_KEY).
Por motivos de segurança, nunca coloque sua chave diretamente no código.

📂 1. Criar o arquivo .env

Na raiz do projeto, crie um arquivo chamado .env com o seguinte conteúdo:

OPENAI_KEY=sua_chave_aqui

⚠️ Substitua sua_chave_aqui pela sua chave real da OpenAI.

🚫 2. Garantir que .env não seja versionado

O arquivo .env já está listado no .gitignore.
Confira se ele contém a entrada abaixo:

.env

Assim, sua chave não será enviada para o repositório remoto.

⚙️ 3. Carregar variáveis no Unity

O projeto já possui um carregador simples de .env.
No Awake() dos scripts que precisam da chave, use:

EnvLoader.Load(); // Carrega o arquivo .env
var openAiApiKey = EnvLoader.Get("OPENAI_KEY");

if (string.IsNullOrEmpty(openAiApiKey))
{
Debug.LogError("OPENAI_KEY não encontrada! Configure o arquivo .env.");
}
else
{
Debug.Log("OPENAI_KEY carregada com sucesso!");
}

✅ Exemplo de fluxo

Criar .env com sua chave.

Iniciar o Unity.

O script vai carregar automaticamente a chave via EnvLoader.

A chave estará disponível em openAiApiKey.

🔥 Pronto! Agora sua chave da OpenAI está segura, configurável e fora do repositório.
