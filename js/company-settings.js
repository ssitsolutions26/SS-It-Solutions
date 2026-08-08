/**
 * Solar ERP - Company Settings Module & Auto-Apply Cache Engine
 */

// --- Default Company Settings Template ---
const DEFAULT_COMPANY_SETTINGS = {
    id: 1,
    companyName: "SS IT Solutions",
    businessName: "SS IT Solutions Private Limited",
    ownerName: "Shashank Sharma",
    logoPath: "assets/logo.png",
    address1: "Plot 102, Innovation Hub, Tech City",
    address2: "Phase 2, Near Metro Station",
    city: "New Delhi",
    state: "Delhi",
    country: "India",
    pincode: "110001",
    gstNo: "07AAAAA0000A1Z5",
    panNo: "ABCDE1234F",
    registrationNo: "REG-2026-SSIT-889",
    
    mobile: "+91 98765 43210",
    alternateMobile: "+91 91234 56789",
    email: "contact@ssitsolutions.com",
    website: "https://ssitsolutions26.github.io/SS-It-Solutions/",
    supportEmail: "support@ssitsolutions.com",
    supportPhone: "+91 1800 123 4567",
    
    bankName: "HDFC Bank",
    accountHolder: "SS IT Solutions Pvt Ltd",
    accountNumber: "50200012345678",
    ifsc: "HDFC0001234",
    branch: "Connaught Place, New Delhi",
    upi: "ssitsolutions@hdfcbank",
    qrCodePath: "",
    
    signaturePath: "",
    
    invoicePrefix: "INV-2026-",
    quotationPrefix: "QTN-2026-",
    receiptPrefix: "REC-2026-",
    billStartNo: 1001,
    quotationStartNo: 501,
    taxType: "exclusive",
    gstPercent: 18,
    cgst: 9,
    sgst: 9,
    igst: 18,
    currency: "₹ (INR)",
    footerText: "Thank you for choosing SS IT Solutions Solar ERP software. Have a sunny day!",
    termsConditions: "1. All disputes subject to Delhi Jurisdiction.\n2. Warranty covers manufacturing defects only.\n3. Goods once sold will not be taken back.",
    
    facebook: "https://facebook.com/ssitsolutions",
    instagram: "https://instagram.com/ssitsolutions",
    whatsApp: "+919876543210",
    youTube: "https://youtube.com/c/ssitsolutions",
    
    updatedDate: new Date().toISOString()
};

// --- In-Memory Company Cache Singleton ---
const CompanyCache = (function() {
    let memoryCache = null;
    const STORAGE_KEY = "SolarERP_CompanySettings";

    function loadFromStorage() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (raw) {
                memoryCache = JSON.parse(raw);
            } else {
                memoryCache = { ...DEFAULT_COMPANY_SETTINGS };
                localStorage.setItem(STORAGE_KEY, JSON.stringify(memoryCache));
            }
        } catch (err) {
            console.error("Error accessing localStorage CompanyCache:", err);
            memoryCache = { ...DEFAULT_COMPANY_SETTINGS };
        }
        return memoryCache;
    }

    return {
        // Load once and return from memory
        get: function() {
            if (!memoryCache) {
                loadFromStorage();
            }
            return memoryCache;
        },

        // Save & Update single active profile
        set: function(newSettings) {
            memoryCache = {
                ...newSettings,
                updatedDate: new Date().toISOString()
            };
            try {
                localStorage.setItem(STORAGE_KEY, JSON.stringify(memoryCache));
            } catch (err) {
                console.error("Failed to save CompanyCache to storage:", err);
            }
            // Trigger auto-apply across ERP interface
            CompanyCache.applyToAllModules();
            return memoryCache;
        },

        // Auto Apply Settings Everywhere
        applyToAllModules: function() {
            const settings = CompanyCache.get();
            
            // 1. Update Navbar / Header Brand
            const headerLogos = document.querySelectorAll('.company-logo-target');
            headerLogos.forEach(img => {
                if (settings.logoPath) img.src = settings.logoPath;
            });

            const headerNames = document.querySelectorAll('.company-name-target');
            headerNames.forEach(el => {
                el.textContent = settings.companyName || "Company Settings";
            });

            // Dispatch global event for other components
            window.dispatchEvent(new CustomEvent('companySettingsUpdated', { detail: settings }));
        }
    };
})();

// --- Toast Notification Helper ---
function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    
    const icon = type === 'success' 
        ? '<svg width="20" height="20" fill="none" stroke="#10B981" stroke-width="2" viewBox="0 0 24 24"><path d="M20 6L9 17l-5-5"/></svg>'
        : '<svg width="20" height="20" fill="none" stroke="#EF4444" stroke-width="2" viewBox="0 0 24 24"><path d="M18 6L6 18M6 6l12 12"/></svg>';

    toast.innerHTML = `${icon} <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}

// --- Image File to Base64 Handler with Validation ---
function handleImageUpload(inputEl, previewImgEl, removeBtnEl, callback) {
    if (!inputEl) return;

    inputEl.addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (!file) return;

        // Validation: PNG, JPG, JPEG only
        const validTypes = ['image/png', 'image/jpeg', 'image/jpg'];
        if (!validTypes.includes(file.type)) {
            showToast('Invalid file format. Please upload PNG, JPG, or JPEG images.', 'error');
            inputEl.value = '';
            return;
        }

        // Validation: Size Limit (Max 2MB)
        if (file.size > 2 * 1024 * 1024) {
            showToast('File size exceeds 2MB limit. Please upload a smaller image.', 'error');
            inputEl.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = function(event) {
            const base64Data = event.target.result;
            if (previewImgEl) {
                previewImgEl.src = base64Data;
                previewImgEl.style.display = 'block';
            }
            if (removeBtnEl) {
                removeBtnEl.style.display = 'inline-flex';
            }
            if (callback) callback(base64Data);
        };
        reader.readAsDataURL(file);
    });
}

// --- Initialize Form Data ---
function loadFormValues() {
    const settings = CompanyCache.get();

    // Populate all text inputs dynamically by matching ID
    Object.keys(settings).forEach(key => {
        const el = document.getElementById(key);
        if (el) {
            if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {
                el.value = settings[key] || '';
            }
        }
    });

    // Image Previews
    if (settings.logoPath) {
        const logoPreview = document.getElementById('logoPreview');
        const removeLogoBtn = document.getElementById('removeLogoBtn');
        if (logoPreview) {
            logoPreview.src = settings.logoPath;
            logoPreview.style.display = 'block';
        }
        if (removeLogoBtn) removeLogoBtn.style.display = 'inline-flex';
    }

    if (settings.qrCodePath) {
        const qrPreview = document.getElementById('qrPreview');
        const removeQrBtn = document.getElementById('removeQrBtn');
        if (qrPreview) {
            qrPreview.src = settings.qrCodePath;
            qrPreview.style.display = 'block';
        }
        if (removeQrBtn) removeQrBtn.style.display = 'inline-flex';
    }

    if (settings.signaturePath) {
        const sigPreview = document.getElementById('sigPreview');
        const removeSigBtn = document.getElementById('removeSigBtn');
        if (sigPreview) {
            sigPreview.src = settings.signaturePath;
            sigPreview.style.display = 'block';
        }
        if (removeSigBtn) removeSigBtn.style.display = 'inline-flex';
    }

    // Auto Apply to initial views
    CompanyCache.applyToAllModules();
}

// --- Save Function ---
function saveCompanySettingsForm(e) {
    if (e) e.preventDefault();

    const companyName = document.getElementById('companyName')?.value.trim();
    const email = document.getElementById('email')?.value.trim();
    const mobile = document.getElementById('mobile')?.value.trim();

    // 1. Validation: Company Name Required
    if (!companyName) {
        showToast('Company Name is required!', 'error');
        document.getElementById('companyName')?.focus();
        return;
    }

    // 2. Email Validation
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        showToast('Please enter a valid Email ID.', 'error');
        document.getElementById('email')?.focus();
        return;
    }

    // 3. Mobile Validation
    if (mobile && !/^[\d\s\+\-\(\)]{7,15}$/.test(mobile)) {
        showToast('Please enter a valid Mobile Number.', 'error');
        document.getElementById('mobile')?.focus();
        return;
    }

    // Extract all field values
    const currentSettings = { ...CompanyCache.get() };
    const fields = [
        'companyName', 'businessName', 'ownerName', 'address1', 'address2', 'city', 'state', 'country', 'pincode',
        'gstNo', 'panNo', 'registrationNo', 'mobile', 'alternateMobile', 'email', 'website', 'supportEmail', 'supportPhone',
        'bankName', 'accountHolder', 'accountNumber', 'ifsc', 'branch', 'upi',
        'invoicePrefix', 'quotationPrefix', 'receiptPrefix', 'billStartNo', 'quotationStartNo', 'taxType', 'gstPercent',
        'cgst', 'sgst', 'igst', 'currency', 'footerText', 'termsConditions',
        'facebook', 'instagram', 'whatsApp', 'youTube'
    ];

    fields.forEach(f => {
        const el = document.getElementById(f);
        if (el) {
            currentSettings[f] = el.value.trim();
        }
    });

    // Save and Update Memory Cache
    CompanyCache.set(currentSettings);

    showToast('Company Settings saved successfully! Auto-applied to all modules.', 'success');
}

// --- Live Invoice & PDF Preview Generator ---
function renderInvoicePreview() {
    const s = CompanyCache.get();

    const logoContainer = document.getElementById('previewInvLogo');
    if (logoContainer) {
        if (s.logoPath) {
            logoContainer.src = s.logoPath;
            logoContainer.style.display = 'block';
        } else {
            logoContainer.style.display = 'none';
        }
    }

    const nameEl = document.getElementById('previewInvCompName');
    if (nameEl) nameEl.textContent = s.companyName || 'COMPANY NAME';

    const metaEl = document.getElementById('previewInvCompMeta');
    if (metaEl) {
        metaEl.innerHTML = `
            ${s.address1 ? s.address1 + ', ' : ''}${s.address2 ? s.address2 + '<br>' : ''}
            ${s.city ? s.city + ', ' : ''}${s.state ? s.state + ' - ' : ''}${s.pincode || ''}<br>
            <strong>GSTIN:</strong> ${s.gstNo || 'N/A'} | <strong>PAN:</strong> ${s.panNo || 'N/A'}<br>
            <strong>Phone:</strong> ${s.mobile || 'N/A'} | <strong>Email:</strong> ${s.email || 'N/A'} | <strong>Web:</strong> ${s.website || 'N/A'}
        `;
    }

    const bankEl = document.getElementById('previewInvBank');
    if (bankEl) {
        bankEl.innerHTML = `
            <strong>Bank:</strong> ${s.bankName || 'N/A'}<br>
            <strong>A/c Holder:</strong> ${s.accountHolder || 'N/A'}<br>
            <strong>A/c No:</strong> ${s.accountNumber || 'N/A'}<br>
            <strong>IFSC:</strong> ${s.ifsc || 'N/A'} | <strong>UPI:</strong> ${s.upi || 'N/A'}
        `;
    }

    const sigImg = document.getElementById('previewInvSig');
    if (sigImg) {
        if (s.signaturePath) {
            sigImg.src = s.signaturePath;
            sigImg.style.display = 'inline-block';
        } else {
            sigImg.style.display = 'none';
        }
    }

    const footerMsg = document.getElementById('previewInvFooterMsg');
    if (footerMsg) footerMsg.textContent = s.footerText || '';

    const termsEl = document.getElementById('previewInvTerms');
    if (termsEl) termsEl.textContent = s.termsConditions || '';

    // Show Modal
    const modal = document.getElementById('previewModal');
    if (modal) modal.classList.add('active');
}

// --- DOM Ready Setup ---
document.addEventListener('DOMContentLoaded', function() {
    loadFormValues();

    // 1. Image Upload Bindings
    const logoInput = document.getElementById('logoInput');
    const logoPreview = document.getElementById('logoPreview');
    const removeLogoBtn = document.getElementById('removeLogoBtn');
    handleImageUpload(logoInput, logoPreview, removeLogoBtn, function(base64) {
        const s = CompanyCache.get();
        s.logoPath = base64;
        CompanyCache.set(s);
    });

    if (removeLogoBtn) {
        removeLogoBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            const s = CompanyCache.get();
            s.logoPath = '';
            CompanyCache.set(s);
            if (logoPreview) logoPreview.style.display = 'none';
            removeLogoBtn.style.display = 'none';
            if (logoInput) logoInput.value = '';
            showToast('Company Logo removed.', 'success');
        });
    }

    const qrInput = document.getElementById('qrInput');
    const qrPreview = document.getElementById('qrPreview');
    const removeQrBtn = document.getElementById('removeQrBtn');
    handleImageUpload(qrInput, qrPreview, removeQrBtn, function(base64) {
        const s = CompanyCache.get();
        s.qrCodePath = base64;
        CompanyCache.set(s);
    });

    if (removeQrBtn) {
        removeQrBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            const s = CompanyCache.get();
            s.qrCodePath = '';
            CompanyCache.set(s);
            if (qrPreview) qrPreview.style.display = 'none';
            removeQrBtn.style.display = 'none';
            if (qrInput) qrInput.value = '';
            showToast('QR Code removed.', 'success');
        });
    }

    const sigInput = document.getElementById('sigInput');
    const sigPreview = document.getElementById('sigPreview');
    const removeSigBtn = document.getElementById('removeSigBtn');
    handleImageUpload(sigInput, sigPreview, removeSigBtn, function(base64) {
        const s = CompanyCache.get();
        s.signaturePath = base64;
        CompanyCache.set(s);
    });

    if (removeSigBtn) {
        removeSigBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            const s = CompanyCache.get();
            s.signaturePath = '';
            CompanyCache.set(s);
            if (sigPreview) sigPreview.style.display = 'none';
            removeSigBtn.style.display = 'none';
            if (sigInput) sigInput.value = '';
            showToast('Digital Signature removed.', 'success');
        });
    }

    // 2. Buttons
    const saveBtn = document.getElementById('saveSettingsBtn');
    if (saveBtn) saveBtn.addEventListener('click', saveCompanySettingsForm);

    const resetBtn = document.getElementById('resetSettingsBtn');
    if (resetBtn) {
        resetBtn.addEventListener('click', function() {
            if (confirm('Are you sure you want to reset to default company settings?')) {
                CompanyCache.set(DEFAULT_COMPANY_SETTINGS);
                loadFormValues();
                showToast('Settings reset to default values.', 'success');
            }
        });
    }

    const previewBtn = document.getElementById('previewSettingsBtn');
    if (previewBtn) {
        previewBtn.addEventListener('click', function() {
            // Save draft values first
            saveCompanySettingsForm();
            renderInvoicePreview();
        });
    }

    const closePreviewBtn = document.getElementById('closePreviewModalBtn');
    if (closePreviewBtn) {
        closePreviewBtn.addEventListener('click', function() {
            const modal = document.getElementById('previewModal');
            if (modal) modal.classList.remove('active');
        });
    }

    // 3. Tab Nav Navigation
    const navItems = document.querySelectorAll('.nav-item');
    navItems.forEach(item => {
        item.addEventListener('click', function() {
            navItems.forEach(n => n.classList.remove('active'));
            this.classList.add('active');
            const targetId = this.getAttribute('data-target');
            if (targetId) {
                const targetCard = document.getElementById(targetId);
                if (targetCard) {
                    targetCard.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        });
    });
});
