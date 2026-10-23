<script setup lang="ts">
import { BarController, BarElement, CategoryScale, Chart, LinearScale, Tooltip, type ChartConfiguration } from 'chart.js'
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { formatMoney, formatWholeMoney } from '@/money'

// Only what a bar chart needs, so the rest of Chart.js stays out of the bundle.
Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip)

const props = defineProps<{
  labels: string[]
  values: number[]
  currency: string
  /** What the chart shows; read out for the canvas, since a table next to it has the figures. */
  label: string
}>()

const canvas = ref<HTMLCanvasElement | null>(null)
let chart: Chart | null = null

function configuration(): ChartConfiguration<'bar'> {
  return {
    type: 'bar',
    data: {
      labels: props.labels,
      datasets: [
        {
          label: props.label,
          data: props.values,
          borderRadius: 10,
          maxBarThickness: 32,
          // The accent runs along the bar like the rest of the dashboard; the labels carry the meaning, not the colour.
          backgroundColor: (context) => {
            const { ctx, chartArea } = context.chart
            if (chartArea === undefined) {
              return '#7c6cf0'
            }
            const gradient = ctx.createLinearGradient(chartArea.left, 0, chartArea.right, 0)
            gradient.addColorStop(0, '#7c6cf0')
            gradient.addColorStop(1, '#4fb4f5')
            return gradient
          },
        },
      ],
    },
    options: {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        tooltip: { callbacks: { label: (item) => formatMoney(Number(item.raw), props.currency) } },
      },
      scales: {
        x: { beginAtZero: true, grid: { display: false }, ticks: { callback: (value) => formatWholeMoney(Number(value), props.currency) } },
        y: { grid: { display: false } },
      },
    },
  }
}

onMounted(() => {
  if (canvas.value !== null) {
    chart = new Chart(canvas.value, configuration())
  }
})

// The parent hands over new arrays on every render; only a change in what they hold redraws the chart.
watch(
  () => JSON.stringify([props.labels, props.values]),
  () => {
    if (chart !== null) {
      chart.data.labels = props.labels
      chart.data.datasets[0]!.data = props.values
      chart.update()
    }
  },
)

onBeforeUnmount(() => chart?.destroy())
</script>

<template>
  <div class="bar-chart" :style="{ height: `${labels.length * 2.75 + 3}rem` }">
    <canvas ref="canvas" role="img" :aria-label="label">{{ label }}: see the table below.</canvas>
  </div>
</template>

<style scoped>
.bar-chart {
  position: relative;
  width: 100%;
}
</style>
