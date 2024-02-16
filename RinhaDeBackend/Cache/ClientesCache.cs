namespace RinhaDeBackend.Cache
{
    public static class ClientesCache
    {
        static Dictionary<int, int> dicionarioClientes = new Dictionary<int, int>();

        public static int GetLimiteCliente(int clienteId)
        {
            if (dicionarioClientes.TryGetValue(clienteId, out int limite))
            {
                return limite;
            }
            return 0;
        }

        public static void SetLimiteCliente(int clienteId, int limite) {
            dicionarioClientes.Add(clienteId, limite);
        }

    }
}