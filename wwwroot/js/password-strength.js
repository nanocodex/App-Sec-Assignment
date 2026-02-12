// Client-side password strength validation and feedback
document.addEventListener('DOMContentLoaded', function () {
    const passwordInput = document.getElementById('password-input');
    
    if (!passwordInput) {
        return; // Exit if not on registration page
    }

    const strengthFeedback = document.getElementById('password-strength-feedback');
    const reqLength = document.getElementById('req-length');
    const reqLowercase = document.getElementById('req-lowercase');
    const reqUppercase = document.getElementById('req-uppercase');
    const reqNumber = document.getElementById('req-number');
    const reqSpecial = document.getElementById('req-special');

    // Real-time password validation on input
    passwordInput.addEventListener('input', function () {
        const password = passwordInput.value;
        let strength = 0;
        let requirementsMet = 0;

        // Check minimum length (12 characters)
        if (password.length >= 12) {
            reqLength.classList.remove('text-danger');
            reqLength.classList.add('text-success');
            strength++;
            requirementsMet++;
        } else {
            reqLength.classList.remove('text-success');
            reqLength.classList.add('text-danger');
        }

        // Check for lowercase letter
        if (/[a-z]/.test(password)) {
            reqLowercase.classList.remove('text-danger');
            reqLowercase.classList.add('text-success');
            strength++;
            requirementsMet++;
        } else {
            reqLowercase.classList.remove('text-success');
            reqLowercase.classList.add('text-danger');
        }

        // Check for uppercase letter
        if (/[A-Z]/.test(password)) {
            reqUppercase.classList.remove('text-danger');
            reqUppercase.classList.add('text-success');
            strength++;
            requirementsMet++;
        } else {
            reqUppercase.classList.remove('text-success');
            reqUppercase.classList.add('text-danger');
        }

        // Check for number
        if (/\d/.test(password)) {
            reqNumber.classList.remove('text-danger');
            reqNumber.classList.add('text-success');
            strength++;
            requirementsMet++;
        } else {
            reqNumber.classList.remove('text-success');
            reqNumber.classList.add('text-danger');
        }

        // Check for special character
        if (/[!@#$%^&*()_+\-=\[\]{}|;:'",.<>?/]/.test(password)) {
            reqSpecial.classList.remove('text-danger');
            reqSpecial.classList.add('text-success');
            strength++;
            requirementsMet++;
        } else {
            reqSpecial.classList.remove('text-success');
            reqSpecial.classList.add('text-danger');
        }

        // Display strength feedback
        if (password.length === 0) {
            strengthFeedback.textContent = '';
            strengthFeedback.className = 'mt-2';
        } else if (requirementsMet === 5 && strength === 5) {
            strengthFeedback.textContent = '? Strong Password';
            strengthFeedback.className = 'mt-2 alert alert-success py-1 px-2';
        } else if (strength >= 3) {
            strengthFeedback.textContent = '? Medium Password - Add more requirements';
            strengthFeedback.className = 'mt-2 alert alert-warning py-1 px-2';
        } else {
            strengthFeedback.textContent = '? Weak Password - Meet all requirements';
            strengthFeedback.className = 'mt-2 alert alert-danger py-1 px-2';
        }
    });

    // Add client-side validation to jQuery Validate
    if ($.validator) {
        $.validator.addMethod('strongpassword', function (value, element) {
            // Minimum length check (12 characters)
            if (value.length < 12) {
                return false;
            }
            // Check for lowercase letter
            if (!/[a-z]/.test(value)) {
                return false;
            }
            // Check for uppercase letter
            if (!/[A-Z]/.test(value)) {
                return false;
            }
            // Check for digit
            if (!/\d/.test(value)) {
                return false;
            }
            // Check for special character
            if (!/[!@#$%^&*()_+\-=\[\]{}|;:'",.<>?/]/.test(value)) {
                return false;
            }
            return true;
        }, 'Password must be at least 12 characters long and contain uppercase, lowercase, numbers, and special characters.');

        // Apply the validation rule to the password input
        $.validator.unobtrusive.adapters.add('strongpassword', function (options) {
            options.rules['strongpassword'] = true;
            options.messages['strongpassword'] = options.message;
        });
    }
});
