document.addEventListener("DOMContentLoaded", function () {
    console.log("✅ JavaScript file loaded");

    // Category Selection
    const categorySelect = document.getElementById("CategorySelect");
    const categoryNamesInput = document.getElementById("CategoryNames");
    const categoryDescriptionsInput = document.getElementById("CategoryDescriptions");

    if (categorySelect) {
        console.log("✅ CategorySelect Found.");
        categorySelect.addEventListener("change", function () {
            console.log("📌 Category selected:", this.value);

            const selectedCategories = Array.from(this.selectedOptions).map(option => option.value);
            const currentCategoryNames = categoryNamesInput ? categoryNamesInput.value.split(',') : [];

            const combinedCategories = [...new Set([...currentCategoryNames, ...selectedCategories])].filter(Boolean);
            if (categoryNamesInput) categoryNamesInput.value = combinedCategories.join(", ");
            if (categoryDescriptionsInput) {
                categoryDescriptionsInput.value = combinedCategories.map(category => category.trim() + " Description").join(", ");
            }
        });
    } else {
        console.warn("⚠️ CategorySelect NOT found!");
    }

    // Tag Selection
    const tagSelect = document.getElementById("TagSelect");
    const tagNamesInput = document.getElementById("TagNames");
    const tagDescriptionsInput = document.getElementById("TagDescriptions");

    if (tagSelect) {
        console.log("✅ TagSelect Found.");
        tagSelect.addEventListener("change", function () {
            console.log("📌 Tag selected:", this.value);

            const selectedTags = Array.from(this.selectedOptions).map(option => option.value);
            const currentTagNames = tagNamesInput ? tagNamesInput.value.split(',') : [];

            const combinedTagNames = [...new Set([...currentTagNames, ...selectedTags])].filter(Boolean);
            if (tagNamesInput) tagNamesInput.value = combinedTagNames.join(", ");
            if (tagDescriptionsInput) {
                tagDescriptionsInput.value = combinedTagNames.map(tag => tag.trim() + " Description").join(", ");
            }
        });
    } else {
        console.warn("⚠️ TagSelect NOT found!");
    }

    // Get Prices
    const getPricesBtn = document.getElementById("getPricesBtn");
    if (getPricesBtn) {
        console.log("✅ getPricesBtn Found.");
        getPricesBtn.addEventListener("click", function () {
            console.log("📌 Fetching electricity prices...");
            fetch('/Home/LoadElectricityComponent')
                .then(response => response.text())
                .then(html => {
                    document.getElementById("electricityPriceResult").innerHTML = html;
                    console.log("✅ Electricity prices loaded.");
                })
                .catch(error => console.error("❌ Error fetching electricity prices:", error));
        });
    } else {
        console.warn("⚠️ getPricesBtn NOT found!");
    }

    // Get Weather
    const getWeatherBtn = document.getElementById("getWeatherBtn");
    const cityInput = document.getElementById("cityInput");
    if (getWeatherBtn && cityInput) {
        console.log("✅ getWeatherBtn Found.");
        getWeatherBtn.addEventListener("click", async function () {
            try {
                const city = cityInput.value;
                if (!city) {
                    alert("Please enter a city.");
                    return;
                }

                console.log("📌 Fetching weather for:", city);
                const response = await fetch(`/Home/LoadWeatherComponent?city=${encodeURIComponent(city)}`);

                if (!response.ok) {
                    alert("City does not exist. Please try again.");
                    return;
                }

                const html = await response.text();
                document.getElementById("weatherResult").innerHTML = html;
                console.log("✅ Weather data loaded.");
            } catch (error) {
                alert("Error processing weather data.");
                console.error("❌ Fetch error:", error);
            }
        });
    } else {
        console.warn("⚠️ getWeatherBtn or cityInput NOT found!");
    }
});
