<template>
  <div class="relative w-full h-full" ref="container">
    <svg ref="svg" class="w-full h-full" />
    <!-- Legend -->
    <div class="absolute top-3 right-3 bg-ink-900/90 border border-ink-600 rounded-lg p-3 text-xs font-mono space-y-1.5">
      <p class="text-gray-400 mb-2 font-display text-xs uppercase tracking-wider">Entity Types</p>
      <div v-for="t in entityTypes" :key="t.type" class="flex items-center gap-2">
        <div class="w-2.5 h-2.5 rounded-full" :style="{ background: t.color }"></div>
        <span class="text-gray-400">{{ t.type }}</span>
      </div>
    </div>
    <!-- Tooltip -->
    <div v-if="tooltip.visible"
      class="absolute pointer-events-none bg-ink-800 border border-signal/30 rounded-lg p-3 text-xs font-mono shadow-xl z-10"
      :style="{ left: tooltip.x + 'px', top: tooltip.y + 'px' }">
      <p class="text-signal font-semibold">{{ tooltip.label }}</p>
      <p class="text-gray-400">Type: {{ tooltip.type }}</p>
      <p v-if="tooltip.risk" class="text-gray-400">Risk: <span class="text-threat-high">{{ tooltip.risk }}%</span></p>
      <p v-if="tooltip.country" class="text-gray-400">Country: {{ tooltip.country }}</p>
    </div>
    <div v-if="!props.nodes?.length" class="absolute inset-0 flex items-center justify-center text-gray-600 font-mono text-sm">
      No entities to display
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, reactive } from 'vue'
import * as d3 from 'd3'
import type { NetworkNode, NetworkEdge } from '@/types'

const props = defineProps<{
  nodes: NetworkNode[]
  edges: NetworkEdge[]
}>()

const svg = ref<SVGSVGElement>()
const container = ref<HTMLDivElement>()

const tooltip = reactive({ visible: false, x: 0, y: 0, label: '', type: '', risk: 0, country: '' })

const colorMap: Record<string, string> = {
  Person:       '#00e5ff',
  Company:      '#a78bfa',
  Account:      '#34c759',
  CryptoWallet: '#ffd60a',
  IPAddress:    '#ff6b35',
  default:      '#6b7280',
}

const entityTypes = Object.entries(colorMap)
  .filter(([k]) => k !== 'default')
  .map(([type, color]) => ({ type, color }))

function nodeColor(type: string) {
  return colorMap[type] ?? colorMap.default
}

function draw() {
  if (!svg.value || !container.value || !props.nodes?.length) return

  const el = svg.value
  d3.select(el).selectAll('*').remove()

  const { width, height } = container.value.getBoundingClientRect()
  const W = width || 600
  const H = height || 400

  const svgSel = d3.select(el)
    .attr('width', W)
    .attr('height', H)

  // Defs: arrow marker
  svgSel.append('defs').append('marker')
    .attr('id', 'arrow')
    .attr('viewBox', '0 -4 8 8')
    .attr('refX', 22).attr('refY', 0)
    .attr('markerWidth', 6).attr('markerHeight', 6)
    .attr('orient', 'auto')
    .append('path')
    .attr('d', 'M0,-4L8,0L0,4')
    .attr('fill', '#00e5ff40')

  const g = svgSel.append('g')

  // Zoom
  svgSel.call(
    d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.3, 3])
      .on('zoom', (e) => g.attr('transform', e.transform))
  )

  const nodes = props.nodes.map(n => ({ ...n })) as any[]
  const links = props.edges.map(e => ({ ...e })) as any[]

  const simulation = d3.forceSimulation(nodes)
    .force('link', d3.forceLink(links).id((d: any) => d.id).distance(120))
    .force('charge', d3.forceManyBody().strength(-400))
    .force('center', d3.forceCenter(W / 2, H / 2))
    .force('collision', d3.forceCollide(30))

  // Links
  const link = g.append('g').selectAll('line')
    .data(links)
    .join('line')
    .attr('stroke', '#1c2b4a')
    .attr('stroke-width', (d: any) => (d.weight ?? 0.5) * 2 + 1)
    .attr('marker-end', 'url(#arrow)')

  // Link labels
  const linkLabel = g.append('g').selectAll('text')
    .data(links)
    .join('text')
    .attr('text-anchor', 'middle')
    .attr('fill', '#4b5563')
    .attr('font-size', 9)
    .attr('font-family', 'JetBrains Mono')
    .text((d: any) => d.label)

  // Node group
  const node = (g.append('g').selectAll('g')
    .data(nodes)
    .join('g')
    .attr('cursor', 'pointer') as any)
    .call(
      d3.drag<SVGGElement, any>()
        .on('start', (e, d) => { if (!e.active) simulation.alphaTarget(0.3).restart(); d.fx = d.x; d.fy = d.y })
        .on('drag', (e, d) => { d.fx = e.x; d.fy = e.y })
        .on('end', (e, d) => { if (!e.active) simulation.alphaTarget(0); d.fx = null; d.fy = null })
    )

  // Outer glow ring
  node.append('circle')
    .attr('r', 20)
    .attr('fill', 'none')
    .attr('stroke', (d: any) => nodeColor(d.type))
    .attr('stroke-width', 1)
    .attr('stroke-opacity', 0.3)

  // Node circle
  node.append('circle')
    .attr('r', 14)
    .attr('fill', (d: any) => nodeColor(d.type) + '20')
    .attr('stroke', (d: any) => nodeColor(d.type))
    .attr('stroke-width', 1.5)

  // Node label
  node.append('text')
    .attr('text-anchor', 'middle')
    .attr('dy', 30)
    .attr('fill', '#d1d5db')
    .attr('font-size', 10)
    .attr('font-family', 'DM Sans')
    .text((d: any) => d.label.length > 18 ? d.label.slice(0, 16) + '…' : d.label)

  // Tooltip on hover
  node
    .on('mouseenter', (event: MouseEvent, d: any) => {
      const rect = container.value!.getBoundingClientRect()
      tooltip.visible = true
      tooltip.x = event.clientX - rect.left + 12
      tooltip.y = event.clientY - rect.top - 10
      tooltip.label = d.label
      tooltip.type = d.type
      tooltip.risk = d.risk ?? 0
      tooltip.country = d.country ?? ''
    })
    .on('mousemove', (event: MouseEvent) => {
      const rect = container.value!.getBoundingClientRect()
      tooltip.x = event.clientX - rect.left + 12
      tooltip.y = event.clientY - rect.top - 10
    })
    .on('mouseleave', () => { tooltip.visible = false })

  simulation.on('tick', () => {
    link
      .attr('x1', (d: any) => d.source.x).attr('y1', (d: any) => d.source.y)
      .attr('x2', (d: any) => d.target.x).attr('y2', (d: any) => d.target.y)

    linkLabel
      .attr('x', (d: any) => (d.source.x + d.target.x) / 2)
      .attr('y', (d: any) => (d.source.y + d.target.y) / 2)

    node.attr('transform', (d: any) => `translate(${d.x},${d.y})`)
  })
}

onMounted(() => { draw() })
watch(() => [props.nodes, props.edges], () => draw(), { deep: true })

const ro = new ResizeObserver(() => draw())
onMounted(() => { if (container.value) ro.observe(container.value) })
onUnmounted(() => ro.disconnect())
</script>
