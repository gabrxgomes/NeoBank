// =============================================
// NeoBank Frontend - JavaScript
// =============================================

// Configuracao da API
const API_URL = 'http://localhost:5120/api';

// Estado da aplicacao
let state = {
    token: localStorage.getItem('neobank_token'),
    user: JSON.parse(localStorage.getItem('neobank_user') || 'null'),
    accounts: [],
    selectedAccountId: null
};

// =============================================
// Inicializacao
// =============================================

document.addEventListener('DOMContentLoaded', () => {
    if (state.token && state.user) {
        showDashboard();
        loadAccounts();
    } else {
        showAuth();
    }
});

// =============================================
// Funcoes de Navegacao
// =============================================

function showAuth() {
    document.getElementById('auth-section').classList.remove('hidden');
    document.getElementById('dashboard-section').classList.add('hidden');
}

function showDashboard() {
    document.getElementById('auth-section').classList.add('hidden');
    document.getElementById('dashboard-section').classList.remove('hidden');
    document.getElementById('user-name').textContent = state.user?.fullName || 'Usuario';
}

function showLogin() {
    document.getElementById('login-form').classList.remove('hidden');
    document.getElementById('register-form').classList.add('hidden');
}

function showRegister() {
    document.getElementById('login-form').classList.add('hidden');
    document.getElementById('register-form').classList.remove('hidden');
}

// =============================================
// Autenticacao
// =============================================

async function handleLogin(event) {
    event.preventDefault();

    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;

    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao fazer login');
        }

        // Salvar token e usuario
        state.token = data.token;
        state.user = {
            id: data.userId,
            fullName: data.fullName,
            email: data.email
        };

        localStorage.setItem('neobank_token', state.token);
        localStorage.setItem('neobank_user', JSON.stringify(state.user));

        showToast('Login realizado com sucesso!', 'success');
        showDashboard();
        loadAccounts();

    } catch (error) {
        showToast(error.message, 'error');
    }
}

async function handleRegister(event) {
    event.preventDefault();

    const fullName = document.getElementById('register-name').value;
    const cpf = document.getElementById('register-cpf').value;
    const email = document.getElementById('register-email').value;
    const phone = document.getElementById('register-phone').value;
    const password = document.getElementById('register-password').value;

    try {
        const response = await fetch(`${API_URL}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fullName, cpf, email, phone, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao cadastrar');
        }

        // Salvar token e usuario
        state.token = data.token;
        state.user = {
            id: data.userId,
            fullName: data.fullName,
            email: data.email
        };

        localStorage.setItem('neobank_token', state.token);
        localStorage.setItem('neobank_user', JSON.stringify(state.user));

        showToast('Cadastro realizado com sucesso!', 'success');
        showDashboard();
        loadAccounts();

    } catch (error) {
        showToast(error.message, 'error');
    }
}

function handleLogout() {
    state.token = null;
    state.user = null;
    state.accounts = [];
    state.selectedAccountId = null;

    localStorage.removeItem('neobank_token');
    localStorage.removeItem('neobank_user');

    showToast('Logout realizado', 'success');
    showAuth();
    showLogin();
}

// =============================================
// Contas
// =============================================

async function loadAccounts() {
    try {
        const response = await fetch(`${API_URL}/accounts`, {
            headers: { 'Authorization': `Bearer ${state.token}` }
        });

        if (!response.ok) {
            if (response.status === 401) {
                handleLogout();
                return;
            }
            throw new Error('Erro ao carregar contas');
        }

        state.accounts = await response.json();
        renderAccounts();

        // Selecionar primeira conta se existir
        if (state.accounts.length > 0 && !state.selectedAccountId) {
            state.selectedAccountId = state.accounts[0].id;
            loadTransactions(state.selectedAccountId);
        }

    } catch (error) {
        showToast(error.message, 'error');
    }
}

function renderAccounts() {
    const container = document.getElementById('accounts-list');

    if (state.accounts.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p>Voce ainda nao possui contas</p>
            </div>
        `;
        return;
    }

    container.innerHTML = state.accounts.map(account => `
        <div class="account-item ${account.id === state.selectedAccountId ? 'selected' : ''}"
             onclick="selectAccount('${account.id}')">
            <div class="account-info">
                <h4>Conta ${account.type}</h4>
                <p>Ag: ${account.agency} | Cc: ${account.accountNumber}</p>
            </div>
            <div class="account-balance">
                <span class="balance ${account.balance < 0 ? 'negative' : ''}">
                    ${formatCurrency(account.balance)}
                </span>
            </div>
        </div>
    `).join('');
}

function selectAccount(accountId) {
    state.selectedAccountId = accountId;
    renderAccounts();
    loadTransactions(accountId);
}

function showCreateAccount() {
    const modalContent = `
        <h2>Nova Conta</h2>
        <form onsubmit="handleCreateAccount(event)">
            <div class="form-group">
                <label for="account-type">Tipo de Conta</label>
                <select id="account-type" required>
                    <option value="1">Conta Corrente</option>
                    <option value="2">Conta Poupanca</option>
                    <option value="3">Conta Investimento</option>
                </select>
            </div>
            <div class="form-group">
                <label for="initial-deposit">Deposito Inicial (opcional)</label>
                <input type="number" id="initial-deposit" min="0" step="0.01" placeholder="0.00">
            </div>
            <div class="modal-actions">
                <button type="button" onclick="closeModal()" class="btn btn-secondary">Cancelar</button>
                <button type="submit" class="btn btn-primary">Criar Conta</button>
            </div>
        </form>
    `;
    showModal(modalContent);
}

async function handleCreateAccount(event) {
    event.preventDefault();

    const type = parseInt(document.getElementById('account-type').value);
    const initialDeposit = parseFloat(document.getElementById('initial-deposit').value) || 0;

    try {
        const response = await fetch(`${API_URL}/accounts`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({ type, initialDeposit })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao criar conta');
        }

        showToast('Conta criada com sucesso!', 'success');
        closeModal();
        loadAccounts();

    } catch (error) {
        showToast(error.message, 'error');
    }
}

// =============================================
// Transacoes
// =============================================

async function loadTransactions(accountId) {
    try {
        const endDate = new Date().toISOString();
        const startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString();

        const response = await fetch(
            `${API_URL}/transactions/statement/${accountId}?startDate=${startDate}&endDate=${endDate}`,
            { headers: { 'Authorization': `Bearer ${state.token}` } }
        );

        if (!response.ok) {
            throw new Error('Erro ao carregar transacoes');
        }

        const data = await response.json();
        renderTransactions(data.transactions);

    } catch (error) {
        console.error(error);
    }
}

function renderTransactions(transactions) {
    const container = document.getElementById('transactions-list');

    if (!transactions || transactions.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p>Nenhuma transacao encontrada</p>
            </div>
        `;
        return;
    }

    container.innerHTML = transactions.slice(0, 10).map(tx => {
        const isCredit = tx.toAccountId === state.selectedAccountId;
        const typeLabel = getTransactionTypeLabel(tx.type);

        return `
            <div class="transaction-item">
                <div class="transaction-info">
                    <h4>${typeLabel}</h4>
                    <p>${tx.description || 'Sem descricao'} - ${formatDate(tx.createdAt)}</p>
                </div>
                <span class="transaction-amount ${isCredit ? 'credit' : 'debit'}">
                    ${isCredit ? '+' : '-'} ${formatCurrency(tx.amount)}
                </span>
            </div>
        `;
    }).join('');
}

function getTransactionTypeLabel(type) {
    const types = {
        'Deposit': 'Deposito',
        'Withdrawal': 'Saque',
        'Transfer': 'Transferencia',
        'Payment': 'Pagamento',
        'PixIn': 'PIX Recebido',
        'PixOut': 'PIX Enviado'
    };
    return types[type] || type;
}

// =============================================
// Operacoes Bancarias
// =============================================

function showDepositModal() {
    if (state.accounts.length === 0) {
        showToast('Crie uma conta primeiro', 'error');
        return;
    }

    const accountOptions = state.accounts.map(a =>
        `<option value="${a.id}" ${a.id === state.selectedAccountId ? 'selected' : ''}>
            ${a.type} - ${a.accountNumber}
        </option>`
    ).join('');

    const modalContent = `
        <h2>Depositar</h2>
        <form onsubmit="handleDeposit(event)">
            <div class="form-group">
                <label for="deposit-account">Conta</label>
                <select id="deposit-account" required>${accountOptions}</select>
            </div>
            <div class="form-group">
                <label for="deposit-amount">Valor</label>
                <input type="number" id="deposit-amount" min="0.01" step="0.01" required placeholder="0.00">
            </div>
            <div class="form-group">
                <label for="deposit-description">Descricao (opcional)</label>
                <input type="text" id="deposit-description" placeholder="Ex: Deposito em dinheiro">
            </div>
            <div class="modal-actions">
                <button type="button" onclick="closeModal()" class="btn btn-secondary">Cancelar</button>
                <button type="submit" class="btn btn-primary">Depositar</button>
            </div>
        </form>
    `;
    showModal(modalContent);
}

async function handleDeposit(event) {
    event.preventDefault();

    const accountId = document.getElementById('deposit-account').value;
    const amount = parseFloat(document.getElementById('deposit-amount').value);
    const description = document.getElementById('deposit-description').value;

    try {
        const response = await fetch(`${API_URL}/transactions/deposit`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({ accountId, amount, description })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao depositar');
        }

        showToast('Deposito realizado com sucesso!', 'success');
        closeModal();
        loadAccounts();
        loadTransactions(state.selectedAccountId);

    } catch (error) {
        showToast(error.message, 'error');
    }
}

function showWithdrawModal() {
    if (state.accounts.length === 0) {
        showToast('Crie uma conta primeiro', 'error');
        return;
    }

    const accountOptions = state.accounts.map(a =>
        `<option value="${a.id}" ${a.id === state.selectedAccountId ? 'selected' : ''}>
            ${a.type} - ${a.accountNumber} (${formatCurrency(a.balance)})
        </option>`
    ).join('');

    const modalContent = `
        <h2>Sacar</h2>
        <form onsubmit="handleWithdraw(event)">
            <div class="form-group">
                <label for="withdraw-account">Conta</label>
                <select id="withdraw-account" required>${accountOptions}</select>
            </div>
            <div class="form-group">
                <label for="withdraw-amount">Valor</label>
                <input type="number" id="withdraw-amount" min="0.01" step="0.01" required placeholder="0.00">
            </div>
            <div class="form-group">
                <label for="withdraw-description">Descricao (opcional)</label>
                <input type="text" id="withdraw-description" placeholder="Ex: Saque caixa eletronico">
            </div>
            <div class="modal-actions">
                <button type="button" onclick="closeModal()" class="btn btn-secondary">Cancelar</button>
                <button type="submit" class="btn btn-primary">Sacar</button>
            </div>
        </form>
    `;
    showModal(modalContent);
}

async function handleWithdraw(event) {
    event.preventDefault();

    const accountId = document.getElementById('withdraw-account').value;
    const amount = parseFloat(document.getElementById('withdraw-amount').value);
    const description = document.getElementById('withdraw-description').value;

    try {
        const response = await fetch(`${API_URL}/transactions/withdraw`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({ accountId, amount, description })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao sacar');
        }

        showToast('Saque realizado com sucesso!', 'success');
        closeModal();
        loadAccounts();
        loadTransactions(state.selectedAccountId);

    } catch (error) {
        showToast(error.message, 'error');
    }
}

function showTransferModal() {
    if (state.accounts.length === 0) {
        showToast('Crie uma conta primeiro', 'error');
        return;
    }

    const accountOptions = state.accounts.map(a =>
        `<option value="${a.id}" ${a.id === state.selectedAccountId ? 'selected' : ''}>
            ${a.type} - ${a.accountNumber} (${formatCurrency(a.balance)})
        </option>`
    ).join('');

    const modalContent = `
        <h2>Transferir</h2>
        <form onsubmit="handleTransfer(event)">
            <div class="form-group">
                <label for="transfer-from">De (Conta Origem)</label>
                <select id="transfer-from" required>${accountOptions}</select>
            </div>
            <div class="form-group">
                <label for="transfer-to">Para (ID da Conta Destino)</label>
                <input type="text" id="transfer-to" required placeholder="ID da conta destino (GUID)">
            </div>
            <div class="form-group">
                <label for="transfer-amount">Valor</label>
                <input type="number" id="transfer-amount" min="0.01" step="0.01" required placeholder="0.00">
            </div>
            <div class="form-group">
                <label for="transfer-description">Descricao (opcional)</label>
                <input type="text" id="transfer-description" placeholder="Ex: Pagamento aluguel">
            </div>
            <div class="modal-actions">
                <button type="button" onclick="closeModal()" class="btn btn-secondary">Cancelar</button>
                <button type="submit" class="btn btn-primary">Transferir</button>
            </div>
        </form>
    `;
    showModal(modalContent);
}

async function handleTransfer(event) {
    event.preventDefault();

    const fromAccountId = document.getElementById('transfer-from').value;
    const toAccountId = document.getElementById('transfer-to').value;
    const amount = parseFloat(document.getElementById('transfer-amount').value);
    const description = document.getElementById('transfer-description').value;

    try {
        const response = await fetch(`${API_URL}/transactions/transfer`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({ fromAccountId, toAccountId, amount, description })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao transferir');
        }

        showToast('Transferencia realizada com sucesso!', 'success');
        closeModal();
        loadAccounts();
        loadTransactions(state.selectedAccountId);

    } catch (error) {
        showToast(error.message, 'error');
    }
}

function showStatementModal() {
    if (state.accounts.length === 0) {
        showToast('Crie uma conta primeiro', 'error');
        return;
    }

    const accountOptions = state.accounts.map(a =>
        `<option value="${a.id}" ${a.id === state.selectedAccountId ? 'selected' : ''}>
            ${a.type} - ${a.accountNumber}
        </option>`
    ).join('');

    const today = new Date().toISOString().split('T')[0];
    const lastMonth = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

    const modalContent = `
        <h2>Extrato</h2>
        <form onsubmit="handleStatement(event)">
            <div class="form-group">
                <label for="statement-account">Conta</label>
                <select id="statement-account" required>${accountOptions}</select>
            </div>
            <div class="form-group">
                <label for="statement-start">Data Inicio</label>
                <input type="date" id="statement-start" required value="${lastMonth}">
            </div>
            <div class="form-group">
                <label for="statement-end">Data Fim</label>
                <input type="date" id="statement-end" required value="${today}">
            </div>
            <div class="modal-actions">
                <button type="button" onclick="closeModal()" class="btn btn-secondary">Fechar</button>
                <button type="submit" class="btn btn-primary">Consultar</button>
            </div>
        </form>
        <div id="statement-result"></div>
    `;
    showModal(modalContent);
}

async function handleStatement(event) {
    event.preventDefault();

    const accountId = document.getElementById('statement-account').value;
    const startDate = document.getElementById('statement-start').value;
    const endDate = document.getElementById('statement-end').value;

    try {
        const response = await fetch(
            `${API_URL}/transactions/statement/${accountId}?startDate=${startDate}&endDate=${endDate}`,
            { headers: { 'Authorization': `Bearer ${state.token}` } }
        );

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao consultar extrato');
        }

        const resultContainer = document.getElementById('statement-result');
        resultContainer.innerHTML = `
            <div class="statement-header">
                <p>Conta: ${data.accountNumber}</p>
                <p class="balance">Saldo: ${formatCurrency(data.currentBalance)}</p>
                <p>Periodo: ${formatDate(data.periodStart)} a ${formatDate(data.periodEnd)}</p>
            </div>
            <div class="transactions-list">
                ${data.transactions.length === 0
                    ? '<p class="empty-state">Nenhuma transacao no periodo</p>'
                    : data.transactions.map(tx => {
                        const isCredit = tx.toAccountId === accountId;
                        return `
                            <div class="transaction-item">
                                <div class="transaction-info">
                                    <h4>${getTransactionTypeLabel(tx.type)}</h4>
                                    <p>${tx.description || 'Sem descricao'} - ${formatDate(tx.createdAt)}</p>
                                </div>
                                <span class="transaction-amount ${isCredit ? 'credit' : 'debit'}">
                                    ${isCredit ? '+' : '-'} ${formatCurrency(tx.amount)}
                                </span>
                            </div>
                        `;
                    }).join('')
                }
            </div>
        `;

    } catch (error) {
        showToast(error.message, 'error');
    }
}

// =============================================
// Modal
// =============================================

function showModal(content) {
    document.getElementById('modal-content').innerHTML = content;
    document.getElementById('modal-overlay').classList.remove('hidden');
}

function closeModal() {
    document.getElementById('modal-overlay').classList.add('hidden');
}

// Fechar modal ao clicar fora
document.getElementById('modal-overlay')?.addEventListener('click', (e) => {
    if (e.target.id === 'modal-overlay') {
        closeModal();
    }
});

// =============================================
// Toast
// =============================================

function showToast(message, type = 'success') {
    const toast = document.getElementById('toast');
    const toastMessage = document.getElementById('toast-message');

    toast.className = `toast ${type}`;
    toastMessage.textContent = message;
    toast.classList.remove('hidden');

    setTimeout(() => {
        toast.classList.add('hidden');
    }, 3000);
}

// =============================================
// Utilidades
// =============================================

function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}
