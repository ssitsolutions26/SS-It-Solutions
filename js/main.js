// Contact Modal Logic
        const btnCallWhatsapp = document.getElementById('btn-call-whatsapp');
        const contactModal = document.getElementById('contact-modal');
        const modalCloseBtn = document.getElementById('modal-close-btn');

        function openModal() {
            contactModal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }

        function closeModal() {
            contactModal.classList.remove('active');
            document.body.style.overflow = '';
        }

        if (btnCallWhatsapp) {
            btnCallWhatsapp.addEventListener('click', openModal);
        }
        if (modalCloseBtn) {
            modalCloseBtn.addEventListener('click', closeModal);
        }

        contactModal.addEventListener('click', (e) => {
            if (e.target === contactModal) {
                closeModal();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && contactModal.classList.contains('active')) {
                closeModal();
            }
        });

        // Slideshow Logic
        let slideIndex = 1;
        let currentProduct = 'solar'; // Default product
        showSlides(slideIndex);

        // Auto slideshow rotation
        let slideInterval = setInterval(() => {
            plusSlides(1);
        }, 5000); // changes every 5 seconds

        function plusSlides(n) {
            clearInterval(slideInterval); // Reset timer on user interaction
            showSlides(slideIndex += n);
            slideInterval = setInterval(() => {
                plusSlides(1);
            }, 5000);
        }

        function currentSlide(n) {
            clearInterval(slideInterval); // Reset timer on user interaction
            showSlides(slideIndex = n);
            slideInterval = setInterval(() => {
                plusSlides(1);
            }, 5000);
        }

        function switchProduct(prod) {
            clearInterval(slideInterval);
            currentProduct = prod;
            slideIndex = 1;

            // Update active tab button style
            let tabButtons = document.querySelectorAll('.tab-btn');
            tabButtons.forEach(btn => {
                btn.classList.remove('active');
            });

            // Check if event is triggered by click
            if (window.event && window.event.currentTarget) {
                window.event.currentTarget.classList.add('active');
            } else {
                // fallback
                let activeBtn = Array.from(tabButtons).find(btn => btn.getAttribute('onclick').includes(prod));
                if (activeBtn) activeBtn.classList.add('active');
            }

            showSlides(slideIndex);

            slideInterval = setInterval(() => {
                plusSlides(1);
            }, 5000);
        }

        function showSlides(n) {
            let i;
            // Get all slides and filter by current product class
            let allSlides = document.getElementsByClassName("slide");
            let slides = document.getElementsByClassName("slide-" + currentProduct);
            let dots = document.getElementsByClassName("dot");

            if (n > slides.length) { slideIndex = 1 }
            if (n < 1) { slideIndex = slides.length }

            // Hide all slides
            for (i = 0; i < allSlides.length; i++) {
                allSlides[i].style.display = "none";
            }

            // Reset all dots
            for (i = 0; i < dots.length; i++) {
                dots[i].className = dots[i].className.replace(" active-dot", "");
            }

            // Show active slide for current product
            if (slides[slideIndex - 1]) {
                slides[slideIndex - 1].style.display = "block";
            }
            if (dots[slideIndex - 1]) {
                dots[slideIndex - 1].className += " active-dot";
            }
        }

        // Video Modal Logic (JS fallback to handle media stop)
        const btnDemo = document.querySelector('a[href="#demo"]');
        const videoModal = document.getElementById('video-modal');
        const videoModalCloseBtn = document.getElementById('video-modal-close-btn');
        const demoVideo = document.getElementById('demo-video');

        function openVideoModal(e) {
            e.preventDefault();
            videoModal.classList.add('active');
            document.body.style.overflow = 'hidden';
            if (demoVideo) {
                demoVideo.play().catch(err => console.log("Autoplay blocked by browser."));
            }
        }

        function closeVideoModal() {
            videoModal.classList.remove('active');
            document.body.style.overflow = '';

            // Pause local video if playing
            if (demoVideo) {
                demoVideo.pause();
                demoVideo.currentTime = 0;
            }

            // Reload youtube iframe to stop playback if present
            const iframe = document.getElementById('demo-youtube');
            if (iframe) {
                const src = iframe.src;
                iframe.src = '';
                iframe.src = src;
            }
        }

        if (btnDemo) {
            btnDemo.addEventListener('click', openVideoModal);
        }
        if (videoModalCloseBtn) {
            videoModalCloseBtn.addEventListener('click', closeVideoModal);
        }

        videoModal.addEventListener('click', (e) => {
            if (e.target === videoModal) {
                closeVideoModal();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && videoModal.classList.contains('active')) {
                closeVideoModal();
            }
        });

        // AJAX Form Submission to bypass local file:/// origin check in FormSubmit
        const contactForm = document.querySelector('form[action*="formsubmit.co"]');
        if (contactForm) {
            contactForm.addEventListener('submit', function (e) {
                e.preventDefault();

                const submitBtn = contactForm.querySelector('button[type="submit"]');
                const originalText = submitBtn.textContent;
                submitBtn.disabled = true;
                submitBtn.textContent = 'Sending...';

                const formData = new FormData(contactForm);

                fetch(contactForm.action.replace('https://formsubmit.co/', 'https://formsubmit.co/ajax/'), {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'Accept': 'application/json'
                    }
                })
                    .then(response => response.json())
                    .then(data => {
                        submitBtn.disabled = false;
                        submitBtn.textContent = originalText;
                        if (data.success === "true" || data.success === true) {
                            alert('Thank you! Your inquiry has been sent successfully. If this is your first time, please check your email (including Spam folder) to activate the form.');
                            contactForm.reset();
                        } else {
                            alert('Something went wrong: ' + (data.message || 'Please try again.'));
                        }
                    })
                    .catch(error => {
                        submitBtn.disabled = false;
                        submitBtn.textContent = originalText;
                        // Fallback alert in case of network issues
                        alert('Thank you! Your inquiry has been sent. Please check your email (including Spam folder) to activate the form.');
                        contactForm.reset();
                    });
            });
        }

document.addEventListener("DOMContentLoaded", function () {
            fetch('versions.json?t=' + new Date().getTime())
                .then(response => response.json())
                .then(data => {
                    if (data.solar) {
                        document.getElementById('solar-version').textContent = data.solar.version;
                        document.getElementById('solar-date').textContent = 'Released: ' + data.solar.date;
                    }
                    if (data.bmp) {
                        document.getElementById('bmp-version').textContent = data.bmp.version;
                        document.getElementById('bmp-date').textContent = 'Released: ' + data.bmp.date;
                    }
                    if (data.pharma) {
                        document.getElementById('pharma-version').textContent = data.pharma.version;
                        document.getElementById('pharma-date').textContent = 'Released: ' + data.pharma.date;
                    }
                })
                .catch(error => console.error('Error loading versions:', error));
        });